// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.Linq;
using Altemiq.SemanticVersioning;

var fileCommandBuilder = new CommandBuilder(new Command("file", "Calculated the differences between two assemblies") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new ConsoleApplication.FileFunctionDelegate(ConsoleApplication.FileFunction)) })
    .AddArgument(new Argument<System.IO.FileInfo>("first") { Description = "The first assembly" })
    .AddArgument(new Argument<System.IO.FileInfo>("second") { Description = "The second assembly" })
    .AddOption(new Option<string>(new string[] { "-b", "--build" }, "Ths build label"));

var solutionCommandBuilder = new CommandBuilder(new Command("solution", "Calculates the version based on a solution file") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new ConsoleApplication.ProcessProjectOrSolutionDelegate(ConsoleApplication.ProcessProjectOrSolution)) })
    .AddArgument(new Argument<System.IO.FileSystemInfo?>("projectOrSolution", GetFileSystemInformation) { Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
    .AddOption(new Option<string?>(new string[] { "--configuration", "-c" }, () => ConsoleApplication.DefaultConfiguration, "The configuration to use for analysing the project. The default for most projects is 'Debug'."))
    .AddOption(new Option<string?>("--platform", () => ConsoleApplication.DefaultPlatform, "The platform to use for analysing the project. The default for most projects is 'AnyCPU'."))
    .AddOption(new Option<string?>("--source", "Specifies the server URL.", ArgumentArity.OneOrMore).WithAlias("-s"))
    .AddOption(new Option<bool>("--no-version-suffix", () => ConsoleApplication.DefaultNoVersionSuffix, "Forces there to be no version suffix. This overrides --version-suffix"))
    .AddOption(new Option<string?>("--version-suffix", () => ConsoleApplication.DefaultVersionSuffix, "Sets the pre-release value. If none is specified, the pre-release from the previous version is used."))
    .AddOption(new Option<bool>("--no-cache", () => ConsoleApplication.DefaultNoCache, "Disable using the machine cache as the first package source."))
    .AddOption(new Option<bool>("--direct-download", () => ConsoleApplication.DefaultDirectDownload, "Download directly without populating any caches with metadata or binaries."))
    .AddOption(new Option<string?>("--package-id-regex", () => ConsoleApplication.DefaultPackageIdRegex, "The regular expression to match in the package id."))
    .AddOption(new Option<string?>("--package-id-replace", () => ConsoleApplication.DefaultPackageIdReplace, "The text used to replace the match from --package-id-regex"))
    .AddOption(new Option<string?>("--package-id", "The package ID to check for previous versions", ArgumentArity.OneOrMore))
    .AddOption(new Option<string?>("--exclude", "A package ID to check exclude from analysis", ArgumentArity.OneOrMore));

var diffCommandBuilder = new CommandBuilder(new Command("diff", "Calculates the differences"))
    .AddCommand(fileCommandBuilder.Command)
    .AddCommand(solutionCommandBuilder.Command)
    .AddGlobalOption(new Option<NuGet.Versioning.SemanticVersion?>(new string[] { "-p", "--previous" }, ParseVersion, isDefault: true, description: "The previous version"))
    .AddGlobalOption(new Option<string>("--build-number-parameter", () => ConsoleApplication.DefaultBuildNumberParameter, "The parameter name for the build number"))
    .AddGlobalOption(new Option<string>("--version-suffix-parameter", () => ConsoleApplication.DefaultVersionSuffixParameter, "The parameter name for the version suffix"))
    .AddGlobalOption(new Option<OutputTypes>("--output", () => ConsoleApplication.DefaultOutput, "The output type"))
    .AddGlobalOption(new Option<bool>(new string[] { "/nologo", "--nologo" }, () => ConsoleApplication.DefaultNoLogo, "Do not display the startup banner or the copyright message."));

var commandLineBuilder = new CommandLineBuilder(new RootCommand(description: "Semantic Version generator"))
    .UseDefaults()
    .UseAnsiTerminalWhenAvailable()
    .AddCommand(diffCommandBuilder.Command);

return await commandLineBuilder
    .Build()
    .InvokeAsync(args)
    .ConfigureAwait(false);

static NuGet.Versioning.SemanticVersion? ParseVersion(ArgumentResult argumentResult)
{
    var tokens = argumentResult.Tokens;
    if (tokens?.Count > 0)
    {
        var value = tokens[0].Value;
        if (NuGet.Versioning.SemanticVersion.TryParse(value, out var version))
        {
            return version;
        }
    }

    return ConsoleApplication.DefaultPrevious;
}

static System.IO.FileSystemInfo? GetFileSystemInformation(ArgumentResult argumentResult)
{
    var pathToken = argumentResult.Tokens.SingleOrDefault();
    if (pathToken is null || pathToken.Value is null)
    {
        return default;
    }

    var path = pathToken.Value;
    if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path))
    {
        return (System.IO.File.GetAttributes(path) & System.IO.FileAttributes.Directory) != 0
            ? new System.IO.DirectoryInfo(path)
            : new System.IO.FileInfo(path);
    }

    argumentResult.ErrorMessage = $"\"{pathToken}\" is not a valid file or directory";
    return default;
}