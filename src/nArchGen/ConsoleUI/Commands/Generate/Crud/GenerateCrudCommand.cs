using Application.Features.Generate.Commands.Crud;
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
        AnsiConsole.WriteLine(value: $"Generating CRUD operations for entity {settings.Entity}");

        IAsyncEnumerable<GeneratedCrudStreamCommandResponse> resultsStream =
            _mediator.CreateStream(request: new GenerateCrudCommandRequest());

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
                                     AnsiConsole.WriteLine(result.LastOperationMessage);
                             }
                         });

        return 0;
    }

    public class Settings : CommandSettings
    {
        [CommandArgument(position: 0, template: "[entity]")]
        public string? Entity { get; set; }
    }
}