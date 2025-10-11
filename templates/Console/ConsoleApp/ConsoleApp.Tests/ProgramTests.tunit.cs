using System.CommandLine;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace ConsoleApp.Tests;

public class ProgramTests
{
    [Test]
    public async Task Invoke_WithHelpOption_DisplaysHelp()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("--help", stdOut);
        
        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stdOut.ToString()).Contains("--help");
    }

    [Test]
    public async Task Invoke_AddWithTwoNumbers_DisplaysResult()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("add 4 2", stdOut);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stdOut.ToString()).Contains("The result is 6");
    }

    private static Task<int> Invoke(string commandLine, StringWriter console)
    {
        RootCommand rootCommand = Program.BuildCommandLine();
        ParseResult parseResult = rootCommand.Parse(commandLine);
        parseResult.InvocationConfiguration.Output = console;
        return parseResult.InvokeAsync();
    }
}
