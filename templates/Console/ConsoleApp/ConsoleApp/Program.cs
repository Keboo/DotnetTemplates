﻿using System.CommandLine;

namespace ConsoleApp;

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        CommandLineConfiguration configuration = GetConfiguration();
        return configuration.InvokeAsync(args);
    }

    public static CommandLineConfiguration GetConfiguration()
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
            parseResult.Configuration.Output.WriteLine($"The result is {result}");
        });

        RootCommand rootCommand = new("A starter console app by Keboo")
        {
            addCommand
        };

        return new CommandLineConfiguration(rootCommand);
    }
}