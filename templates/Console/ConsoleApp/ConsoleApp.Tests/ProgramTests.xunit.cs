//#if (mcp)
namespace ConsoleApp.Tests;

public class ProgramTests
{
    [Fact]
    public void Add_WithTwoNumbers_DisplaysResult()
    {
        MathTools tool = new();

        string result = tool.Add(4, 2);

        Assert.Equal("The result is 6", result);
    }

    [Fact]
    public void Add_WithNegativeNumber_DisplaysResult()
    {
        MathTools tool = new();

        string result = tool.Add(4, -2);

        Assert.Equal("The result is 2", result);
    }
}
//#else
using System.CommandLine;

namespace ConsoleApp.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Invoke_WithHelpOption_DisplaysHelp()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("--help", stdOut);

        Assert.Equal(0, exitCode);
        Assert.Contains("--help", stdOut.ToString());
    }

    [Fact]
    public async Task Invoke_AddWithTwoNumbers_DisplaysResult()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("add 4 2", stdOut);

        Assert.Equal(0, exitCode);
        Assert.Contains("The result is 6", stdOut.ToString());
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
