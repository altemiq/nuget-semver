// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using Altemiq.SemanticVersioning;

var noLogoOption = new CliOption<bool>("/nologo", "--nologo") { Description = "Do not display the startup banner or the copyright message.", DefaultValueFactory = _ => ConsoleApplication.DefaultNoLogo, Recursive = true };

var root = new CliRootCommand(description: "Semantic Version generator")
{
    CreateDiffCommand(noLogoOption),
    noLogoOption,
};

var configuration = new CliConfiguration(root);

return await configuration
    .InvokeAsync(args)
    .ConfigureAwait(false);

static CliCommand CreateDiffCommand(CliOption<bool> noLogoOption)
{
    var outputTypesOption = new CliOption<OutputTypes>("--output") { Description = "The output type", DefaultValueFactory = _ => ConsoleApplication.DefaultOutput, Recursive = true };
    var previousOption = new CliOption<NuGet.Versioning.SemanticVersion?>("-p", "--previous") { Description = "The previous version", CustomParser = ParseVersion, DefaultValueFactory = ParseVersion, Recursive = true };
    var buildNumberParameterOption = new CliOption<string>("--build-number-parameter") { Description = "The parameter name for the build number", DefaultValueFactory = _ => ConsoleApplication.DefaultBuildNumberParameter, Recursive = true };
    var versionSuffixParameterOption = new CliOption<string>("--version-suffix-parameter") { Description = "The parameter name for the version suffix", DefaultValueFactory = _ => ConsoleApplication.DefaultVersionSuffixParameter, Recursive = true };
    var incrementOption = new CliOption<SemanticVersionIncrement>("--increment") { Description = "The location to increment the version", DefaultValueFactory = _ => default, Recursive = true };

    var command = new CliCommand("diff", "Calculates the differences")
    {
        CreateFileCommand(previousOption, outputTypesOption, buildNumberParameterOption, versionSuffixParameterOption, incrementOption, noLogoOption),
        CreateSolutionCommand(previousOption, outputTypesOption, buildNumberParameterOption, versionSuffixParameterOption, incrementOption, noLogoOption),
        previousOption,
        buildNumberParameterOption,
        versionSuffixParameterOption,
        outputTypesOption,
        incrementOption,
    };

    return command;

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

    static CliCommand CreateFileCommand(
        CliOption<NuGet.Versioning.SemanticVersion?> previousOption,
        CliOption<OutputTypes> outputTypesOption,
        CliOption<string> buildNumberParameterOption,
        CliOption<string> versionSuffixParameterOption,
        CliOption<SemanticVersionIncrement> incrementOption,
        CliOption<bool> noLogoOption)
    {
        var firstArgument = new CliArgument<FileInfo>("first") { Description = "The first assembly" };
        var secondArgument = new CliArgument<FileInfo>("second") { Description = "The second assembly" };
        var buildOption = new CliOption<string>("-b", "--build") { Description = "Ths build label" };

        var command = new CliCommand("file", "Calculated the differences between two assemblies")
        {
            firstArgument,
            secondArgument,
            buildOption,
        };

        command.SetAction(parseResult => ConsoleApplication.FileFunction(
            new SystemConsoleTerminal(ConsoleApplication.Console.Create(parseResult.Configuration.Output, parseResult.Configuration.Error)),
            parseResult.GetValue(firstArgument)!,
            parseResult.GetValue(secondArgument)!,
            parseResult.GetValue(buildOption),
            parseResult.GetValue(previousOption) ?? throw new MissingFieldException(),
            parseResult.GetValue(outputTypesOption),
            parseResult.GetValue(buildNumberParameterOption) ?? ConsoleApplication.DefaultBuildNumberParameter,
            parseResult.GetValue(versionSuffixParameterOption) ?? ConsoleApplication.DefaultVersionSuffixParameter,
            parseResult.GetValue(incrementOption),
            parseResult.GetValue(noLogoOption)));

        return command;
    }

    static CliCommand CreateSolutionCommand(
        CliOption<NuGet.Versioning.SemanticVersion?> previousOption,
        CliOption<OutputTypes> outputTypesOption,
        CliOption<string> buildNumberParameterOption,
        CliOption<string> versionSuffixParameterOption,
        CliOption<SemanticVersionIncrement> incrementOption,
        CliOption<bool> noLogoOption)
    {
        var projectOrSolutionArgument = new CliArgument<FileSystemInfo?>("projectOrSolution") { Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one.", CustomParser = GetFileSystemInformation, HelpName = "PROJECT | SOLUTION" };
        var configurationOption = new CliOption<string?>("--configuration", "-c") { Description = "The configuration to use for analysing the project. The default for most projects is 'Debug'.", DefaultValueFactory = _ => ConsoleApplication.DefaultConfiguration, HelpName = "CONFIGURATON" };
        var platformOption = new CliOption<string?>("--platform") { Description = "The platform to use for analysing the project. The default for most projects is 'AnyCPU'.", DefaultValueFactory = _ => ConsoleApplication.DefaultPlatform, HelpName = "PLATFORM" };
        var sourceOption = new CliOption<string[]>("--source", "-s") { Description = "Specifies the server URL.", HelpName = "SOURCE" };
        var packageIdRegexOption = new CliOption<string?>("--package-id-regex") { Description = "The regular expression to match in the package id.", DefaultValueFactory = _ => ConsoleApplication.DefaultPackageIdRegex };
        var packageIdReplaceOption = new CliOption<string?>("--package-id-replace") { Description = "The text used to replace the match from --package-id-regex", DefaultValueFactory = _ => ConsoleApplication.DefaultPackageIdReplace };
        var packageIdOption = new CliOption<string[]>("--package-id") { Description = "The package ID to check for previous versions" };
        var excludeOption = new CliOption<string[]>("--exclude") { Description = "A package ID to check exclude from analysis", HelpName = "EXCLUDE" };
        var noVersionSuffixOption = new CliOption<bool>("--no-version-suffix") { Description = "Forces there to be no version suffix. This overrides --version-suffix", DefaultValueFactory = _ => ConsoleApplication.DefaultNoVersionSuffix };
        var versionSuffixOption = new CliOption<string>("--version-suffix") { Description = "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.", DefaultValueFactory = _ => ConsoleApplication.DefaultVersionSuffix, HelpName = "VERSION_SUFFIX" };
        var noCacheOption = new CliOption<bool>("--no-cache") { Description = "Disable using the machine cache as the first package source.", DefaultValueFactory = _ => ConsoleApplication.DefaultNoCache };
        var directDownloadOption = new CliOption<bool>("--direct-download") { Description = "Download directly without populating any caches with metadata or binaries.", DefaultValueFactory = _ => ConsoleApplication.DefaultDirectDownload };
        var commitCountOption = new CliOption<int>("--commit-count") { Description = "The number of commits to analyse for equivalent packages", DefaultValueFactory = _ => ConsoleApplication.DefaultCommitCount, HelpName = "COMMIT_COUNT" };
        var forceOption = new CliOption<bool>("--force", "-f") { Description = "Force the computation of the version" };

        var command = new CliCommand("solution", "Calculates the version based on a solution file")
        {
            projectOrSolutionArgument,
            configurationOption,
            platformOption,
            sourceOption,
            packageIdRegexOption,
            packageIdReplaceOption,
            packageIdOption,
            excludeOption,
            noVersionSuffixOption,
            versionSuffixOption,
            noCacheOption,
            directDownloadOption,
            commitCountOption,
            forceOption,
        };

        command.SetAction((parseResult, _) => ConsoleApplication.ProcessProjectOrSolution(
            new SystemConsoleTerminal(ConsoleApplication.Console.Create(parseResult.Configuration.Output, parseResult.Configuration.Error)),
            parseResult.GetValue(projectOrSolutionArgument),
            parseResult.GetValue(sourceOption) ?? Enumerable.Empty<string>(),
            parseResult.GetValue(packageIdOption) ?? Enumerable.Empty<string>(),
            parseResult.GetValue(excludeOption) ?? Enumerable.Empty<string>(),
            parseResult.GetValue(configurationOption),
            parseResult.GetValue(platformOption),
            parseResult.GetValue(packageIdRegexOption),
            parseResult.GetValue(packageIdReplaceOption),
            parseResult.GetValue(versionSuffixOption),
            parseResult.GetValue(noVersionSuffixOption),
            parseResult.GetValue(noCacheOption),
            parseResult.GetValue(directDownloadOption),
            parseResult.GetValue(commitCountOption),
            parseResult.GetValue(previousOption),
            parseResult.GetValue(outputTypesOption),
            parseResult.GetValue(buildNumberParameterOption) ?? ConsoleApplication.DefaultBuildNumberParameter,
            parseResult.GetValue(versionSuffixParameterOption) ?? ConsoleApplication.DefaultVersionSuffixParameter,
            parseResult.GetValue(incrementOption),
            parseResult.GetValue(noLogoOption),
            parseResult.GetValue(forceOption)));

        return command;

        static FileSystemInfo? GetFileSystemInformation(ArgumentResult argumentResult)
        {
            var pathToken = argumentResult.Tokens.SingleOrDefault();
            if (pathToken?.Value is null)
            {
                return default;
            }

            var path = pathToken.Value;
            if (File.Exists(path) || Directory.Exists(path))
            {
                const FileAttributes None = default;
                return (File.GetAttributes(path) & FileAttributes.Directory) != None
                    ? new System.IO.DirectoryInfo(path)
                    : new System.IO.FileInfo(path);
            }

            argumentResult.AddError($"\"{pathToken}\" is not a valid file or directory");
            return default;
        }
    }
}