// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Parsing;
using Altemiq.SemanticVersioning;

var noLogoOption = new Option<bool>("/nologo", "--nologo") { Description = "Do not display the startup banner or the copyright message.", DefaultValueFactory = _ => ConsoleApplication.DefaultNoLogo, Recursive = true };

var root = new RootCommand(description: "Semantic Version generator")
{
    CreateDiffCommand(noLogoOption),
    noLogoOption,
};

return await root
    .Parse(args)
    .InvokeAsync()
    .ConfigureAwait(false);

static Command CreateDiffCommand(Option<bool> noLogoOption)
{
    var outputTypesOption = new Option<OutputTypes>("--output") { Description = "The output type", DefaultValueFactory = _ => ConsoleApplication.DefaultOutput, Recursive = true };
    var previousOption = new Option<NuGet.Versioning.SemanticVersion?>("-p", "--previous") { Description = "The previous version", CustomParser = ParseVersion, DefaultValueFactory = ParseVersion, Recursive = true };
    var buildNumberParameterOption = new Option<string>("--build-number-parameter") { Description = "The parameter name for the build number", DefaultValueFactory = _ => ConsoleApplication.DefaultBuildNumberParameter, Recursive = true };
    var versionSuffixParameterOption = new Option<string>("--version-suffix-parameter") { Description = "The parameter name for the version suffix", DefaultValueFactory = _ => ConsoleApplication.DefaultVersionSuffixParameter, Recursive = true };
    var incrementOption = new Option<SemanticVersionIncrement>("--increment") { Description = "The location to increment the version", DefaultValueFactory = _ => default, Recursive = true };

    var command = new Command("diff", "Calculates the differences")
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
        if (tokens is null or { Count: 0 })
        {
            return ConsoleApplication.DefaultPrevious;
        }

        var value = tokens[0].Value;
        return NuGet.Versioning.SemanticVersion.TryParse(value, out var version)
            ? version
            : ConsoleApplication.DefaultPrevious;
    }

    static Command CreateFileCommand(
        Option<NuGet.Versioning.SemanticVersion?> previousOption,
        Option<OutputTypes> outputTypesOption,
        Option<string> buildNumberParameterOption,
        Option<string> versionSuffixParameterOption,
        Option<SemanticVersionIncrement> incrementOption,
        Option<bool> noLogoOption)
    {
        var firstArgument = new Argument<FileInfo>("first") { Description = "The first assembly" };
        var secondArgument = new Argument<FileInfo>("second") { Description = "The second assembly" };
        var buildOption = new Option<string>("-b", "--build") { Description = "Ths build label" };

        var command = new Command("file", "Calculated the differences between two assemblies")
        {
            firstArgument,
            secondArgument,
            buildOption,
        };

        command.SetAction(parseResult => ConsoleApplication.FileFunction(
            ConsoleApplication.Console.Create(parseResult.InvocationConfiguration.Output, parseResult.InvocationConfiguration.Error, parseResult.GetValue(outputTypesOption)),
            parseResult.GetValue(firstArgument)!,
            parseResult.GetValue(secondArgument)!,
            parseResult.GetValue(buildOption),
            parseResult.GetValue(previousOption) ?? throw new MissingFieldException(),
            parseResult.GetValue(buildNumberParameterOption) ?? ConsoleApplication.DefaultBuildNumberParameter,
            parseResult.GetValue(versionSuffixParameterOption) ?? ConsoleApplication.DefaultVersionSuffixParameter,
            parseResult.GetValue(incrementOption),
            parseResult.GetValue(noLogoOption)));

        return command;
    }

    static Command CreateSolutionCommand(
        Option<NuGet.Versioning.SemanticVersion?> previousOption,
        Option<OutputTypes> outputTypesOption,
        Option<string> buildNumberParameterOption,
        Option<string> versionSuffixParameterOption,
        Option<SemanticVersionIncrement> incrementOption,
        Option<bool> noLogoOption)
    {
        var projectOrSolutionArgument = new Argument<FileSystemInfo?>("projectOrSolution") { Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one.", CustomParser = GetFileSystemInformation, HelpName = "PROJECT | SOLUTION", Arity = ArgumentArity.ZeroOrOne };
        var configurationOption = new Option<string?>("--configuration", "-c") { Description = "The configuration to use for analysing the project. The default for most projects is 'Debug'.", DefaultValueFactory = _ => ConsoleApplication.DefaultConfiguration, HelpName = "CONFIGURATION" };
        var platformOption = new Option<string?>("--platform") { Description = "The platform to use for analysing the project. The default for most projects is 'AnyCPU'.", DefaultValueFactory = _ => ConsoleApplication.DefaultPlatform, HelpName = "PLATFORM" };
        var sourceOption = new Option<string[]>("--source", "-s") { Description = "Specifies the server URL.", HelpName = "SOURCE" };
        var packageIdRegexOption = new Option<string?>("--package-id-regex") { Description = "The regular expression to match in the package id.", DefaultValueFactory = _ => ConsoleApplication.DefaultPackageIdRegex };
        var packageIdReplaceOption = new Option<string?>("--package-id-replace") { Description = "The text used to replace the match from --package-id-regex", DefaultValueFactory = _ => ConsoleApplication.DefaultPackageIdReplace };
        var packageIdOption = new Option<string[]>("--package-id") { Description = "The package ID to check for previous versions" };
        var excludeOption = new Option<string[]>("--exclude") { Description = "A package ID to check exclude from analysis", HelpName = "EXCLUDE" };
        var noVersionSuffixOption = new Option<bool>("--no-version-suffix") { Description = "Forces there to be no version suffix. This overrides --version-suffix", DefaultValueFactory = _ => ConsoleApplication.DefaultNoVersionSuffix };
        var versionSuffixOption = new Option<string>("--version-suffix") { Description = "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.", DefaultValueFactory = _ => ConsoleApplication.DefaultVersionSuffix, HelpName = "VERSION_SUFFIX" };
        var noCacheOption = new Option<bool>("--no-cache") { Description = "Disable using the machine cache as the first package source.", DefaultValueFactory = _ => ConsoleApplication.DefaultNoCache };
        var directDownloadOption = new Option<bool>("--direct-download") { Description = "Download directly without populating any caches with metadata or binaries.", DefaultValueFactory = _ => ConsoleApplication.DefaultDirectDownload };
        var commitCountOption = new Option<int>("--commit-count") { Description = "The number of commits to analyse for equivalent packages", DefaultValueFactory = _ => ConsoleApplication.DefaultCommitCount, HelpName = "COMMIT_COUNT" };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Force the computation of the version", DefaultValueFactory = _ => ConsoleApplication.DefaultForce };

        var command = new Command("solution", "Calculates the version based on a solution file")
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
            ConsoleApplication.Console.Create(parseResult.InvocationConfiguration.Output, parseResult.InvocationConfiguration.Error, parseResult.GetValue(outputTypesOption)),
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
                return (File.GetAttributes(path) & FileAttributes.Directory) is not None
                    ? new DirectoryInfo(path)
                    : new FileInfo(path);
            }

            argumentResult.AddError($"\"{pathToken}\" is not a valid file or directory");
            return default;
        }
    }
}