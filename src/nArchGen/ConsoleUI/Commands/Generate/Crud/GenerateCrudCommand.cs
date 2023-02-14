using Application.Features.Generate.Commands.Crud;
using Core.CodeGen.Code.CSharp;
using Core.CodeGen.Code.CSharp.ValueObjects;
using Domain.ValueObjects;
using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ConsoleUI.Commands.Generate.Crud;

public class GenerateCrudCommand : AsyncCommand<GenerateCrudCommand.Settings>
{
    private readonly IMediator _mediator;

    public GenerateCrudCommand(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(paramName: nameof(mediator));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        settings.CheckEntityArgument();
        settings.CheckMechanismOptions();
        settings.CheckDbContextArgument();

        ICollection<PropertyInfo> entityProperties =
            await CSharpCodeReader.ReadClassPropertiesAsync(
                @$"{Environment.CurrentDirectory}\Domain\Entities\{settings.EntityName}.cs");
        GenerateCrudCommandRequest generateCrudCommand = new()
        {
            CrudTemplateData = new CrudTemplateData
            {
                Entity = new Entity
                {
                    Name = settings.EntityName!,
                    Properties = entityProperties.Where(property => property.AccessModifier == "public").ToArray()
                },
                IsCachingUsed = settings.IsCachingUsed,
                IsLoggingUsed = settings.IsLoggingUsed,
                IsTransactionUsed = settings.IsTransactionUsed,
                IsSecuredOperationUsed = settings.IsSecuredOperationUsed,
                DbContextName = settings.DbContextName!
            }
        };

        IAsyncEnumerable<GeneratedCrudStreamCommandResponse> resultsStream =
            _mediator.CreateStream(request: generateCrudCommand);

        await AnsiConsole.Status()
                         .Spinner(Spinner.Known.Dots2)
                         .SpinnerStyle(style: Style.Parse(text: "blue"))
                         .StartAsync(status: "Generating...", action: async ctx =>
                         {
                             await foreach
                                 (GeneratedCrudStreamCommandResponse result in resultsStream)
                             {
                                 ctx.Status(result.CurrentStatusMessage);
                                 if (result.LastOperationMessage is not null)
                                     AnsiConsole.MarkupLine($":check_mark_button: {result.LastOperationMessage}");


                                 if (result.NewFilePathsResult is not null)
                                 {
                                     AnsiConsole.MarkupLine(":new_button: [green]Generated files:[/]");
                                     foreach (string filePath in result.NewFilePathsResult)
                                         AnsiConsole.Write(new TextPath(filePath).StemColor(Color.Yellow)
                                                               .LeafColor(Color.Blue));
                                 }

                                 if (result.UpdatedFilePathsResult is not null)
                                 {
                                     AnsiConsole.MarkupLine(":up_button: [green]Updated files:[/]");
                                     foreach (string filePath in result.UpdatedFilePathsResult)
                                         AnsiConsole.Write(new TextPath(filePath).StemColor(Color.Yellow)
                                                               .LeafColor(Color.Blue));
                                 }
                             }
                         });


        return 0;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[entity]")]
        public string? EntityName { get; set; }

        [CommandOption("-c|--caching")] public bool IsCachingUsed { get; set; }

        [CommandOption("-l|--logging")] public bool IsLoggingUsed { get; set; }

        [CommandOption("-t|--transaction")] public bool IsTransactionUsed { get; set; }
        [CommandOption("-s|--secured")] public bool IsSecuredOperationUsed { get; set; }
        [CommandOption("-d|--dbcontext")] public string? DbContextName { get; set; }

        public void CheckEntityArgument()
        {
            if (EntityName is not null)
            {
                AnsiConsole.MarkupLine($"Selected [green]entity[/] is [blue]{EntityName}[/].");
                return;
            }

            if (EntityName is null)
            {
                string[] entities = Directory.GetFiles(path: "Domain\\Entities")
                                             .Select(Path.GetFileNameWithoutExtension)
                                             .ToArray()!;
                if (entities.Length == 0)
                {
                    AnsiConsole.MarkupLine("[red]No entities found in Domain\\Entities[/]");
                    return;
                }

                EntityName = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                                .Title("What's your [green]entity[/]?")
                                                .PageSize(5)
                                                .AddChoices(entities));
            }
        }

        public void CheckDbContextArgument()
        {
            if (DbContextName is not null)
            {
                AnsiConsole.MarkupLine($"Selected [green]DbContext[/] is [blue]{DbContextName}[/].");
                return;
            }

            if (DbContextName is null)
            {
                string[] dbContexts = Directory.GetFiles(path: @$"{Environment.CurrentDirectory}\Persistence\Contexts")
                                               .Select(Path.GetFileNameWithoutExtension)
                                               .ToArray()!;
                if (dbContexts.Length == 0)
                {
                    AnsiConsole.MarkupLine(@"[red]No DbContexts found in 'Persistence\Contexts'[/]");
                    return;
                }

                DbContextName = AnsiConsole.Prompt(new SelectionPrompt<string>()
                                                   .Title("What's your [green]DbContext[/]?")
                                                   .PageSize(5)
                                                   .AddChoices(dbContexts));
            }
        }

        public void CheckMechanismOptions()
        {
            List<string> mechanismsToPrompt = new();

            if (IsCachingUsed)
                AnsiConsole.MarkupLine("[green]Caching[/] is used.");
            else mechanismsToPrompt.Add("Caching");
            if (IsLoggingUsed)
                AnsiConsole.MarkupLine("[green]Logging[/] is used.");
            else mechanismsToPrompt.Add("Logging");
            if (IsTransactionUsed)
                AnsiConsole.MarkupLine("[green]Transaction[/] is used.");
            else mechanismsToPrompt.Add("Transaction");
            if (IsSecuredOperationUsed)
                AnsiConsole.MarkupLine("[green]SecuredOperation[/] is used.");
            else mechanismsToPrompt.Add("Secured Operation");

            if (mechanismsToPrompt.Count == 0) return;

            List<string> selectedMechanisms = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("What [green]mechanisms[/] do you want to use?")
                    .NotRequired()
                    .PageSize(5)
                    .MoreChoicesText("[grey](Move up and down to reveal more mechanisms)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a mechanism, " +
                        "[green]<enter>[/] to accept)[/]")
                    .AddChoices(mechanismsToPrompt));

            selectedMechanisms.ToList().ForEach(mechanism =>
            {
                switch (mechanism)
                {
                    case "Caching":
                        IsCachingUsed = true;
                        break;
                    case "Logging":
                        IsLoggingUsed = true;
                        break;
                    case "Transaction":
                        IsTransactionUsed = true;
                        break;
                    case "Secured Operation":
                        IsSecuredOperationUsed = true;
                        break;
                }
            });
        }
    }
}