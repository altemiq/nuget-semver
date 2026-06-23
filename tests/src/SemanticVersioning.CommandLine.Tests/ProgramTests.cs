// -----------------------------------------------------------------------
// <copyright file="ProgramTests.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.SemanticVersioning.CommandLine;

public class ProgramTests
{
    private async static Task<(int, IEnumerable<string>, IEnumerable<string>)> InvokeAsync(params string[] args)
    {
        var rootCommand = Program.CreateRootCommand();
        var configuration = new System.CommandLine.ParserConfiguration();

        var parsed = rootCommand.Parse(args, configuration);
        var output = new ConsoleWriter(parsed.InvocationConfiguration.Output);
        parsed.InvocationConfiguration.Output = output;

        var error = new ConsoleWriter(parsed.InvocationConfiguration.Error);
        parsed.InvocationConfiguration.Error = error;

        // get the current out/error
        var exitValue = await parsed.InvokeAsync();

        return (exitValue, output.CapturedOutput, error.CapturedOutput);
    }

    [Test]
    public async Task NoValidNuget()
    {
        var (exitValue, console, error) = await InvokeAsync("diff", "solution", GetProjectPath("Original"), "--source", GetSource("no-packages"), "--no-cache", "--nologo");
        await Assert.That(error).IsEmpty();
        await Assert.That(exitValue).IsDefault();
        await Assert.That(console).Contains("##teamcity[buildNumber '1.0.0']");
        await Assert.That(console).Contains("##teamcity[setParameter name='system.build.suffix' value='alpha']");
    }

    [Test]
    public async Task NameChange()
    {
        var (exitValue, console, error) = await InvokeAsync("diff", "solution", GetProjectPath("New"), "--source", GetSource("only-release"), "--no-cache", "--package-id-regex", "New", "--package-id-replace", "Original", "--nologo");
        await Assert.That(error).IsEmpty();
        await Assert.That(exitValue).IsDefault();
        await Assert.That(console).Contains("##teamcity[buildNumber '2.0.0']");
        await Assert.That(console).Contains("##teamcity[setParameter name='system.build.suffix' value='']");
    }

    [Test]
    public async Task NoFullRelease()
    {
        var (exitValue, console, error) = await InvokeAsync("diff", "solution", GetProjectPath("Original"), "--source", GetSource("only-prerelease"), "--no-cache", "--nologo");
        await Assert.That(error).IsEmpty();
        await Assert.That(exitValue).IsDefault();
        await Assert.That(console).Contains("##teamcity[buildNumber '1.0.2']");
        await Assert.That(console).Contains("##teamcity[setParameter name='system.build.suffix' value='develop']");
    }

    [Test]
    public async Task NoPreRelease()
    {
        var (exitValue, console, error) = await InvokeAsync("diff", "solution", GetProjectPath("Original"), "--source", GetSource("only-release"), "--direct-download", "--no-cache", "--nologo");
        await Assert.That(error).IsEmpty();
        await Assert.That(exitValue).IsDefault();
        await Assert.That(console).Contains("##teamcity[buildNumber '1.0.1']");
        await Assert.That(console).Contains("##teamcity[setParameter name='system.build.suffix' value='']");
    }

    [Test]
    public async Task PreReleaseAndRelease()
    {
        var (exitValue, console, error) = await InvokeAsync("diff", "solution", GetProjectPath("Original"), "--source", GetSource("full"), "--direct-download", "--no-cache", "--nologo");
        await Assert.That(error).IsEmpty();
        await Assert.That(exitValue).IsDefault();
        await Assert.That(console).Contains("##teamcity[buildNumber '1.0.2']");
        await Assert.That(console).Contains("##teamcity[setParameter name='system.build.suffix' value='']");
    }

    private static string GetProjectPath(string project) => Path.GetFullPath(Path.Combine(GetProjectDirectory(project), Path.GetFileName(project) + ".csproj"));

    private static string GetProjectDirectory(string project)
    {
        var projectFolder = GetFolderAbove(GetTestDirectory(), "projects") ?? throw new DirectoryNotFoundException("projects");
        return Path.GetFullPath(Path.Combine(projectFolder, project));
    }

    private static string GetSource(string source)
    {
        var nupkgFolder = GetFolderAbove(GetTestDirectory(), "nupkg") ?? throw new DirectoryNotFoundException("nupkg");
        return Path.GetFullPath(Path.Combine(nupkgFolder!, source));
    }

    private static string GetTestDirectory() => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(typeof(ProgramTests).Assembly.Location)))) ?? throw new InvalidOperationException("Failed to get test directory");

    private static string? GetFolderAbove(string? current, string name)
    {
        while (current is not null)
        {
            var test = Path.Combine(current, name);
            if (Directory.Exists(test))
            {
                return test;
            }

            current = Path.GetDirectoryName(current);
        }

        return default;
    }

    private class ConsoleWriter(TextWriter original) : TextWriter
    {
        private readonly System.Text.StringBuilder stringBuilder = new();

        public ICollection<string> CapturedOutput => this.stringBuilder.Length == 0
            ? []
            : this.stringBuilder.ToString().Split(Environment.NewLine);

        public TextWriter Original { get; } = original;

        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(string? value) => this.stringBuilder.Append(value);

        public override void WriteLine(string? value) => this.stringBuilder.AppendLine(value);
    }
}