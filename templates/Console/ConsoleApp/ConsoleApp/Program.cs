using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace ConsoleApp;

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        return Invoke(args, new SystemConsole());
    }

    public static Task<int> Invoke(string[] args, IConsole console)
    {
        Argument<int> number1 = new("number1", "The first number to add");
        Argument<int> number2 = new("number2", "The second number to add");
        Command addCommand = new("add", "Add two numbers together")
        {
            number1,
            number2
        };
        addCommand.SetHandler((InvocationContext context) =>
        {
            int value1 = context.ParseResult.GetValueForArgument(number1);
            int value2 = context.ParseResult.GetValueForArgument(number2);
            int result = value1 + value2;
            console.Out.WriteLine($"The result is {result}");
            return Task.FromResult(0);
        });

        RootCommand rootCommand = new("A starter console app by Keboo")
        {
            addCommand
        };
        return rootCommand.InvokeAsync(args, console);
    }
}