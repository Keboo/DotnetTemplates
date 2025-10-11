using System.CommandLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConsoleApp.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public async Task Invoke_WithHelpOption_DisplaysHelp()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("--help", stdOut);
        
        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(stdOut.ToString().Contains("--help"));
    }

    [TestMethod]
    public async Task Invoke_AddWithTwoNumbers_DisplaysResult()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("add 4 2", stdOut);

        Assert.AreEqual(0, exitCode);
        Assert.IsTrue(stdOut.ToString().Contains("The result is 6"));
    }

    private static Task<int> Invoke(string commandLine, StringWriter console)
    {
        RootCommand rootCommand = Program.BuildCommandLine();
        ParseResult parseResult = rootCommand.Parse(commandLine);
        parseResult.InvocationConfiguration.Output = console;
        return parseResult.InvokeAsync();
    }
}
