using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;

namespace ConsoleApp.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Invoke_WithHelpOption_DisplaysHelp()
    {
        TestConsole console = new();
        int exitCode = await Invoke("--help", console);

        Assert.Equal(0, exitCode);
        Assert.Contains("--help", console.Out.ToString());
    }

    [Fact]
    public async Task Invoke_AddWithTwoNumbers_DisplaysResult()
    {
        TestConsole console = new();
        int exitCode = await Invoke("add 4 2", console);

        Assert.Equal(0, exitCode);
        Assert.Contains("The result is 6", console.Out.ToString());
    }

    private static Task<int> Invoke(string commandLine, IConsole console) 
        => Program.Invoke(CommandLineStringSplitter.Instance.Split(commandLine).ToArray(), console);
}