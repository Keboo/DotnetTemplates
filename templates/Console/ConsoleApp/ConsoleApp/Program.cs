//#if (mcp)
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ConsoleApp;

public sealed class Program
{
    private static Task Main(string[] args)
    {
        // NuGet MCP packaging guidance:
        // https://learn.microsoft.com/nuget/concepts/nuget-mcp
        // MCP C# SDK docs:
        // https://github.com/modelcontextprotocol/csharp-sdk
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        return builder.Build().RunAsync();
    }
}

[McpServerToolType]
public sealed class MathTools
{
    [McpServerTool, Description("Adds two numbers together.")]
    public string Add(
        [Description("The first number to add.")] int number1,
        [Description("The second number to add.")] int number2)
    {
        int result = number1 + number2;
        return $"The result is {result}";
    }
}
//#else
using System.CommandLine;

namespace ConsoleApp;

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        RootCommand rootCommand = BuildCommandLine();
        return rootCommand.Parse(args).InvokeAsync();
    }

    public static RootCommand BuildCommandLine()
    {
        Argument<int> number1 = new("number1")
        {
            Description = "The first number to add"
        };
        Argument<int> number2 = new("number2")
        {
            Description = "The second number to add"
        };
        Command addCommand = new("add", "Add two numbers together")
        {
            number1,
            number2
        };

        addCommand.SetAction((ParseResult parseResult) =>
        {
            int value1 = parseResult.CommandResult.GetValue(number1);
            int value2 = parseResult.CommandResult.GetValue(number2);
            int result = value1 + value2;
            parseResult.InvocationConfiguration.Output.WriteLine($"The result is {result}");
        });

        RootCommand rootCommand = new("A starter console app by Keboo")
        {
            addCommand
        };

        return rootCommand;
    }
}
//#endif
