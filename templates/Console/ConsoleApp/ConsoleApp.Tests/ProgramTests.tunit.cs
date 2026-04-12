using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

//#if (mcp)
namespace ConsoleApp.Tests;

public class ProgramTests
{
    [Test]
    public async Task Add_WithTwoNumbers_DisplaysResult()
    {
        MathTools tool = new();

        string result = tool.Add(4, 2);

        await Assert.That(result).IsEqualTo("The result is 6");
    }

    [Test]
    public async Task Add_WithNegativeNumber_DisplaysResult()
    {
        MathTools tool = new();

        string result = tool.Add(4, -2);

        await Assert.That(result).IsEqualTo("The result is 2");
    }
}
//#else
using System.CommandLine;

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
//#endif
