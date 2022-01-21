// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Mondo">
// Copyright (c) Mondo. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using Mondo.SemanticVersioning;

var noLogoOption = new Option<bool>(new string[] { "/nologo", "--nologo" }, () => ConsoleApplication.DefaultNoLogo, "Do not display the startup banner or the copyright message.");

var root = new RootCommand(description: "Semantic Version generator")
{
    CreateDiffCommand(noLogoOption),
};

root.AddGlobalOption(noLogoOption);

var commandLineBuilder = new CommandLineBuilder(root)
    .UseDefaults()
    .UseAnsiTerminalWhenAvailable();

return await commandLineBuilder
    .Build()
    .InvokeAsync(args)
    .ConfigureAwait(false);

static Command CreateDiffCommand(Option<bool> noLogoOption)
{
    var outputTypesOption = new Option<OutputTypes>("--output", () => ConsoleApplication.DefaultOutput, "The output type");
    var previousOption = new Option<NuGet.Versioning.SemanticVersion?>(new string[] { "-p", "--previous" }, ParseVersion, isDefault: true, description: "The previous version");
    var buildNumberParameterOption = new Option<string>("--build-number-parameter", () => ConsoleApplication.DefaultBuildNumberParameter, "The parameter name for the build number");
    var versionSuffixParameterOption = new Option<string>("--version-suffix-parameter", () => ConsoleApplication.DefaultVersionSuffixParameter, "The parameter name for the version suffix");
    var incrementOption = new Option<SemanticVersionIncrement>("--increment",  () => default(SemanticVersionIncrement), "The location to increment the version");

    var command = new Command("diff", "Calculates the differences")
    {
        CreateFileCommand(previousOption, outputTypesOption, buildNumberParameterOption, versionSuffixParameterOption, incrementOption, noLogoOption),
        CreateSolutionCommand(previousOption, outputTypesOption, buildNumberParameterOption, versionSuffixParameterOption, incrementOption, noLogoOption),
    };

    command.AddGlobalOption(previousOption);
    command.AddGlobalOption(buildNumberParameterOption);
    command.AddGlobalOption(versionSuffixParameterOption);
    command.AddGlobalOption(outputTypesOption);
    command.AddGlobalOption(incrementOption);

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

    static Command CreateFileCommand(
        Option<NuGet.Versioning.SemanticVersion?> previousOption,
        Option<OutputTypes> outputTypesOption,
        Option<string> buildNumberParameterOption,
        Option<string> versionSuffixParameterOption,
        Option<SemanticVersionIncrement> incrementOption,
        Option<bool> noLogoOption)
    {
        var firstArgument = new Argument<FileInfo>("first", "The first assembly");
        var secondArgument = new Argument<FileInfo>("second", "The second assembly");
        var buildOption = new Option<string>(new string[] { "-b", "--build" }, "Ths build label");

        var command = new Command("file", "Calculated the differences between two assemblies")
        {
            firstArgument,
            secondArgument,
            buildOption,
        };

        var optionsHandler = new FileFunctionsOptionsHandler(
            firstArgument,
            secondArgument,
            buildOption);

        command.SetHandler<IConsole, ConsoleApplication.FileFunctionOptions, NuGet.Versioning.SemanticVersion, OutputTypes, string, string, SemanticVersionIncrement, bool>(
            ConsoleApplication.FileFunction,
            optionsHandler,
            previousOption,
            outputTypesOption,
            buildNumberParameterOption,
            versionSuffixParameterOption,
            incrementOption,
            noLogoOption);

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
        var projectOrSolutionArgument = new Argument<FileSystemInfo?>("projectOrSolution", GetFileSystemInformation, description: "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one.") { HelpName = "PROJECT | SOLUTION", Name = "PROJECT | SOLUTION" };
        var configurationOption = new Option<string?>(new string[] { "--configuration", "-c" }, () => ConsoleApplication.DefaultConfiguration, "The configuration to use for analysing the project. The default for most projects is 'Debug'.") { ArgumentHelpName = "CONFIGURATON" };
        var platformOption = new Option<string?>("--platform", () => ConsoleApplication.DefaultPlatform, "The platform to use for analysing the project. The default for most projects is 'AnyCPU'.") { ArgumentHelpName = "PLATFORM" };
        var sourceOption = new Option<string[]>(new string[] { "--source", "-s" }, "Specifies the server URL.") { ArgumentHelpName = "SOURCE" };
        var packageIdRegexOption = new Option<string?>("--package-id-regex", () => ConsoleApplication.DefaultPackageIdRegex, "The regular expression to match in the package id.");
        var packageIdReplaceOption = new Option<string?>("--package-id-replace", () => ConsoleApplication.DefaultPackageIdReplace, "The text used to replace the match from --package-id-regex");
        var packageIdOption = new Option<string[]>("--package-id", "The package ID to check for previous versions");
        var excludeOption = new Option<string[]>("--exclude", "A package ID to check exclude from analysis") { ArgumentHelpName = "EXCLUDE" };
        var noVersionSuffixOption = new Option<bool>("--no-version-suffix", () => ConsoleApplication.DefaultNoVersionSuffix, "Forces there to be no version suffix. This overrides --version-suffix");
        var versionSuffixOption = new Option<string>("--version-suffix", () => ConsoleApplication.DefaultVersionSuffix, "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.") { ArgumentHelpName = "VERSION_SUFFIX" };
        var noCacheOption = new Option<bool>("--no-cache", () => ConsoleApplication.DefaultNoCache, "Disable using the machine cache as the first package source.");
        var directDownloadOption = new Option<bool>("--direct-download", () => ConsoleApplication.DefaultDirectDownload, "Download directly without populating any caches with metadata or binaries.");
        var commitCountOption = new Option<int>("--commit-count", () => ConsoleApplication.DefaultCommitCount, "The number of commits to analyse for equivalent packages") { ArgumentHelpName = "COMMIT_COUNT" };

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
        };

        var optionsHandler = new ProcessProjectOrSolutionOptionsHandler(
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
            commitCountOption);

        command.SetHandler<IConsole, ConsoleApplication.ProcessProjectOrSolutionOptions, NuGet.Versioning.SemanticVersion?, OutputTypes, string, string, SemanticVersionIncrement, bool>(
            ConsoleApplication.ProcessProjectOrSolution,
            optionsHandler,
            previousOption,
            outputTypesOption,
            buildNumberParameterOption,
            versionSuffixParameterOption,
            incrementOption,
            noLogoOption);

        return command;

        static FileSystemInfo? GetFileSystemInformation(ArgumentResult argumentResult)
        {
            var pathToken = argumentResult.Tokens.SingleOrDefault();
            if (pathToken.Value is null)
            {
                return default;
            }

            var path = pathToken.Value;
            if (File.Exists(path) || Directory.Exists(path))
            {
                return (File.GetAttributes(path) & FileAttributes.Directory) != 0
                    ? new System.IO.DirectoryInfo(path)
                    : new System.IO.FileInfo(path);
            }

            argumentResult.ErrorMessage = $"\"{pathToken}\" is not a valid file or directory";
            return default;
        }
    }
}

/// <content>
/// The binder base classes.
/// </content>
internal partial class Program
{
    private sealed class FileFunctionsOptionsHandler : System.CommandLine.Binding.BinderBase<ConsoleApplication.FileFunctionOptions>
    {
        private readonly Argument<FileInfo> firstArgument;
        private readonly Argument<FileInfo> secondArgument;
        private readonly Option<string> buildOption;

        public FileFunctionsOptionsHandler(Argument<FileInfo> firstArgument, Argument<FileInfo> secondArgument, Option<string> buildOption)
        {
            this.firstArgument = firstArgument;
            this.secondArgument = secondArgument;
            this.buildOption = buildOption;
        }

        /// <inheritdoc/>
        protected override ConsoleApplication.FileFunctionOptions GetBoundValue(System.CommandLine.Binding.BindingContext bindingContext) => new()
        {
            First = bindingContext.ParseResult.GetValueForArgument(this.firstArgument)!,
            Second = bindingContext.ParseResult.GetValueForArgument(this.secondArgument)!,
            Build = bindingContext.ParseResult.GetValueForOption(this.buildOption),
        };
    }

    private sealed class ProcessProjectOrSolutionOptionsHandler : System.CommandLine.Binding.BinderBase<ConsoleApplication.ProcessProjectOrSolutionOptions>
    {
        private readonly Argument<FileSystemInfo?> projectOrSolutionArgument;
        private readonly Option<string?> configurationOption;
        private readonly Option<string?> platformOption;
        private readonly Option<string[]> sourceOption;
        private readonly Option<string?> packageIdRegexOption;
        private readonly Option<string?> packageIdReplaceOption;
        private readonly Option<string[]> packageIdOption;
        private readonly Option<string[]> excludeOption;
        private readonly Option<bool> noVersionSuffixOption;
        private readonly Option<string> versionSuffixOption;
        private readonly Option<bool> noCacheOption;
        private readonly Option<bool> directDownloadOption;
        private readonly Option<int> commitCountOption;

        public ProcessProjectOrSolutionOptionsHandler(
            Argument<FileSystemInfo?> projectOrSolutionArgument,
            Option<string?> configurationOption,
            Option<string?> platformOption,
            Option<string[]> sourceOption,
            Option<string?> packageIdRegexOption,
            Option<string?> packageIdReplaceOption,
            Option<string[]> packageIdOption,
            Option<string[]> excludeOption,
            Option<bool> noVersionSuffixOption,
            Option<string> versionSuffixOption,
            Option<bool> noCacheOption,
            Option<bool> directDownloadOption,
            Option<int> commitCountOption)
        {
            this.projectOrSolutionArgument = projectOrSolutionArgument;
            this.configurationOption = configurationOption;
            this.platformOption = platformOption;
            this.sourceOption = sourceOption;
            this.packageIdRegexOption = packageIdRegexOption;
            this.packageIdReplaceOption = packageIdReplaceOption;
            this.packageIdOption = packageIdOption;
            this.excludeOption = excludeOption;
            this.noVersionSuffixOption = noVersionSuffixOption;
            this.versionSuffixOption = versionSuffixOption;
            this.noCacheOption = noCacheOption;
            this.directDownloadOption = directDownloadOption;
            this.commitCountOption = commitCountOption;
        }

        protected override ConsoleApplication.ProcessProjectOrSolutionOptions GetBoundValue(System.CommandLine.Binding.BindingContext bindingContext) => new()
        {
            ProjectOrSolution = bindingContext.ParseResult.GetValueForArgument(this.projectOrSolutionArgument),
            Source = bindingContext.ParseResult.GetValueForOption(this.sourceOption) ?? Enumerable.Empty<string>(),
            PackageId = bindingContext.ParseResult.GetValueForOption(this.packageIdOption) ?? Enumerable.Empty<string>(),
            Exclude = bindingContext.ParseResult.GetValueForOption(this.excludeOption) ?? Enumerable.Empty<string>(),
            Configuration = bindingContext.ParseResult.GetValueForOption(this.configurationOption),
            Platform = bindingContext.ParseResult.GetValueForOption(this.platformOption),
            PackageIdRegex = bindingContext.ParseResult.GetValueForOption(this.packageIdRegexOption),
            PackageIdReplace = bindingContext.ParseResult.GetValueForOption(this.packageIdReplaceOption),
            VersionSuffix = bindingContext.ParseResult.GetValueForOption(this.versionSuffixOption),
            NoVersionSuffix = bindingContext.ParseResult.GetValueForOption(this.noVersionSuffixOption),
            NoCache = bindingContext.ParseResult.GetValueForOption(this.noCacheOption),
            DirectDownload = bindingContext.ParseResult.GetValueForOption(this.directDownloadOption),
            CommitCount = bindingContext.ParseResult.GetValueForOption(this.commitCountOption),
        };
    }
}