// -----------------------------------------------------------------------
// <copyright file="ProgramTests.cs" company="Altavec">
// Copyright (c) Altavec. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altavec.SemanticVersioning.CommandLine;

public class ProgramTests
{
    private static readonly object lockObject = new();

    private static readonly System.Reflection.MethodInfo MainMethod;

    static ProgramTests()
    {
        lock (lockObject)
        {
            MainMethod ??= EntryPointDiscoverer.FindStaticEntryMethod(typeof(ConsoleApplication).Assembly);
        }
    }

    private static (int Return, IEnumerable<string> Output, IEnumerable<string> Error) Invoke(params string[] args)
    {
        var consoleOut = new ConsoleWriter(Console.Out);
        Console.SetOut(consoleOut);

        var consoleError = new ConsoleWriter(Console.Error);
        Console.SetError(consoleError);

        var returnObject = MainMethod.Invoke(null, new object[] { args });
        var (exitValue, console, error) = returnObject switch
        {
            Task<int> intTask => Return(intTask.Result),
            Task task => WaitThenReturn(task),
            int intValue => Return(intValue),
            _ => Return(),
        };

        Console.SetOut(consoleOut.Original);
        Console.SetError(consoleError.Original);

        return (exitValue, console, error);

        (int Return, IEnumerable<string> Output, IEnumerable<string> Error) WaitThenReturn(Task task, int value = 0)
        {
            task.Wait();
            return Return(value);
        }

        (int Return, IEnumerable<string> Output, IEnumerable<string> Error) Return(int value = 0)
        {
            return (value, consoleOut.CapturedOutput, consoleError.CapturedOutput);
        }
    }

    [Fact]
    public void NoValidNuget()
    {
        var (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(Path.Combine("Projects", "Original")), "--source", GetSource("NoPackages"), "--no-cache", "--nologo");
        exitValue.Should().Be(0);
        console.Should().Contain("##teamcity[buildNumber '1.0.0']").And.Contain("##teamcity[setParameter name='system.build.suffix' value='alpha']");
        error.Should().BeEmpty();
    }

    [Fact]
    public void NameChange()
    {
        var (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(Path.Combine("Projects", "New")), "--source", GetSource("OnlyRelease"), "--no-cache", "--package-id-regex", "New", "--package-id-replace", "Original", "--nologo");
        exitValue.Should().Be(0);
        console.Should().Contain("##teamcity[buildNumber '2.0.0']").And.Contain("##teamcity[setParameter name='system.build.suffix' value='']");
        error.Should().BeEmpty();
    }

    [Fact]
    public void NoFullRelease()
    {
        var (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(Path.Combine("Projects", "Original")), "--source", GetSource("OnlyPrerelease"), "--no-cache", "--nologo");
        exitValue.Should().Be(0);
        console.Should().Contain("##teamcity[buildNumber '1.0.2']").And.Contain("##teamcity[setParameter name='system.build.suffix' value='develop']");
        error.Should().BeEmpty();
    }

    [Fact]
    public void NoPreRelease()
    {
        var (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(Path.Combine("Projects", "Original")), "--source", GetSource("OnlyRelease"), "--direct-download", "--no-cache", "--nologo");
        exitValue.Should().Be(0);
        console.Should().Contain("##teamcity[buildNumber '1.0.1']").And.Contain("##teamcity[setParameter name='system.build.suffix' value='']");
        error.Should().BeEmpty();
    }

    [Fact]
    public void PreReleaseAndRelease()
    {
        var (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(Path.Combine("Projects", "Original")), "--source", GetSource("Full"), "--direct-download", "--no-cache", "--nologo");
        exitValue.Should().Be(0);
        console.Should().Contain("##teamcity[buildNumber '1.0.2']").And.Contain("##teamcity[setParameter name='system.build.suffix' value='']");
        error.Should().BeEmpty();
    }

    private static string GetProjectPath(string project) => Path.GetFullPath(Path.Combine(GetProjectDirectory(project), Path.GetFileName(project) + ".csproj"));

    private static string GetProjectDirectory(string project) => Path.GetFullPath(Path.Combine(GetTestDirectory(), "..", project));

    private static string GetSource(string source) => Path.GetFullPath(Path.Combine(GetTestDirectory(), "..", "NuPkg", source));

    private static string GetTestDirectory() => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(typeof(ProgramTests).Assembly.Location)))) ?? throw new InvalidOperationException("Failed to get test directory");


    private class ConsoleWriter : TextWriter
    {
        private readonly System.Text.StringBuilder stringBuilder = new();

        public ConsoleWriter(TextWriter original) => this.Original = original;

        public IEnumerable<string> CapturedOutput => this.stringBuilder.Length == 0
            ? Enumerable.Empty<string>()
            : this.stringBuilder.ToString().Split(Environment.NewLine);

        public TextWriter Original { get; }

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(string? value) => this.stringBuilder.Append(value);

        public override void WriteLine(string? value) => this.stringBuilder.AppendLine(value);
    }
}
