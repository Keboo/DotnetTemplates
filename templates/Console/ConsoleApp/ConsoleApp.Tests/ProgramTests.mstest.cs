using Microsoft.VisualStudio.TestTools.UnitTesting;

//#if (mcp)
namespace ConsoleApp.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public void Add_WithTwoNumbers_DisplaysResult()
    {
        MathTools tool = new();

        string result = tool.Add(4, 2);

        Assert.AreEqual("The result is 6", result);
    }

    [TestMethod]
    public void Add_WithNegativeNumber_DisplaysResult()
    {
        MathTools tool = new();

        string result = tool.Add(4, -2);

        Assert.AreEqual("The result is 2", result);
    }
}
//#else
using System.CommandLine;

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
//#endif
