using System.Text;
using Application;
using ConsoleUI.Commands.Generate.Crud;
using Core.ConsoleUI.IoC.SpectreConsoleCli;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

#region Console Configuration

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

#endregion

#region IoC

IServiceCollection services = new ServiceCollection();
services.AddApplicationServices();
TypeRegistrar registrar = new(services);

#endregion

CommandApp app = new(registrar);
app.Configure(config =>
{
    #region Controller

    config.AddBranch(name: "generate", generate =>
    {
        generate.AddCommand<GenerateCrudCommand>(name: "crud")
                .WithDescription(description: "Generate CRUD operations for entity")
                .WithExample(args: new[] { "generate", "crud", "User" });
    });

    #endregion
});

return app.Run(args);