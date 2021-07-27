// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Linq;
using Mondo.SemanticVersioning;

var fileCommandBuilder = new CommandBuilder(new Command("file", "Calculated the differences between two assemblies") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new Application.FileFunctionDelegate(Application.FileFunction)) })
    .AddArgument(new Argument<System.IO.FileInfo>("first") { Description = "The first assembly" })
    .AddArgument(new Argument<System.IO.FileInfo>("second") { Description = "The second assembly" })
    .AddOption(new Option<string>(new string[] { "-b", "--build" }, "Ths build label"));

var solutionCommandBuilder = new CommandBuilder(new Command("solution", "Calculates the version based on a solution file") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new MSBuildApplication.ProcessProjectOrSolutionDelegate(MSBuildApplication.ProcessProjectOrSolution)) })
    .AddArgument(new Argument<System.IO.FileSystemInfo?>("projectOrSolution", GetFileSystemInformation) { Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
    .AddOption(new Option<string?>(new string[] { "--configuration", "-c" }, () => MSBuildApplication.DefaultConfiguration, "The configuration to use for analysing the project. The default for most projects is 'Debug'."))
    .AddOption(new Option<string?>("--platform", () => MSBuildApplication.DefaultPlatform, "The platform to use for analysing the project. The default for most projects is 'AnyCPU'."))
    .AddOption(new Option<string?>("--source", "Specifies the server URL.", ArgumentArity.OneOrMore).WithAlias("-s"))
    .AddOption(new Option<bool>("--no-version-suffix", () => MSBuildApplication.DefaultNoVersionSuffix, "Forces there to be no version suffix. This overrides --version-suffix"))
    .AddOption(new Option<string?>("--version-suffix", () => MSBuildApplication.DefaultVersionSuffix, "Sets the pre-release value. If none is specified, the pre-release from the previous version is used."))
    .AddOption(new Option<bool>("--no-cache", () => MSBuildApplication.DefaultNoCache, "Disable using the machine cache as the first package source."))
    .AddOption(new Option<bool>("--direct-download", () => MSBuildApplication.DefaultDirectDownload, "Download directly without populating any caches with metadata or binaries."))
    .AddOption(new Option<string?>("--package-id-regex", () => MSBuildApplication.DefaultPackageIdRegex, "The regular expression to match in the package id."))
    .AddOption(new Option<string>("--package-id-replace", () => MSBuildApplication.DefaultPackageIdReplace, "The text used to replace the match from --package-id-regex"))
    .AddOption(new Option<string?>("--package-id", "The package ID to check for previous versions", ArgumentArity.OneOrMore))
    .AddOption(new Option<string?>("--exclude", "A package ID to check exclude from analysis", ArgumentArity.OneOrMore));

var diffCommandBuilder = new CommandBuilder(new Command("diff", "Calculates the differences"))
    .AddCommand(fileCommandBuilder.Command)
    .AddCommand(solutionCommandBuilder.Command)
    .AddGlobalOption(new Option<NuGet.Versioning.SemanticVersion?>(new string[] { "-p", "--previous" }, ParseVersion, isDefault: true, description: "The previous version"))
    .AddGlobalOption(new Option<string>("--build-number-parameter", () => Application.DefaultBuildNumberParameter, "The parameter name for the build number"))
    .AddGlobalOption(new Option<string>("--version-suffix-parameter", () => Application.DefaultVersionSuffixParameter, "The parameter name for the version suffix"))
    .AddGlobalOption(new Option<OutputTypes>("--output", () => Application.DefaultOutput, "The output type"))
    .AddGlobalOption(new Option<bool>(new string[] { "/nologo", "--nologo" }, () => Application.DefaultNoLogo, "Do not display the startup banner or the copyright message."));

var commandLineBuilder = new CommandLineBuilder(new RootCommand(description: "Semantic Version generator"))
    .UseDefaults()
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

    return MSBuildApplication.DefaultPrevious;
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