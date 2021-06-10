// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Altemiq.SemanticVersioning.Specs")]

namespace Altemiq.SemanticVersioning
{
    using System;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The program class.
    /// </summary>
    internal static partial class Program
   {
        private delegate Task<int> ProcessProjectOrSolutionDelegate(
            System.IO.FileSystemInfo projectOrSolution,
            string? configuration,
            string? platform,
            System.Collections.Generic.IEnumerable<string> source,
            System.Collections.Generic.IEnumerable<string> packageId,
            System.Collections.Generic.IEnumerable<string> exclude,
            string? packageIdRegex,
            string packageIdReplace,
            string? versionSuffix,
            NuGet.Versioning.SemanticVersion? previous,
            bool noVersionSuffix,
            bool noCache,
            bool directDownload,
            bool noLogo,
            OutputTypes output,
            string buildNumberParameter,
            string versionSuffixParameter);

        private static Task<int> Main(string[] args)
        {
            var fileCommandBuilder = new CommandBuilder(new Command("file", "Calculated the differences between two assemblies") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new Action<System.IO.FileInfo, System.IO.FileInfo, NuGet.Versioning.SemanticVersion, string, OutputTypes, string, string, bool>(Application.FileFunction)) })
                .AddArgument(new Argument<System.IO.FileInfo>("first") { Description = "The first assembly" })
                .AddArgument(new Argument<System.IO.FileInfo>("second") { Description = "The second assembly" })
                .AddOption(new Option<string>(new string[] { "-b", "--build" }, "Ths build label"));

            var solutionCommandBuilder = new CommandBuilder(new Command("solution", "Calculates the version based on a solution file") { Handler = System.CommandLine.Invocation.CommandHandler.Create(new ProcessProjectOrSolutionDelegate(Application.ProcessProjectOrSolution)) })
                .AddArgument(new Argument<System.IO.FileSystemInfo?>("projectOrSolution", GetFileSystemInformation) { Description = "The project or solution file to operate on. If a file is not specified, the command will search the current directory for one." })
                .AddOption(new Option<string?>(new string[] { "-c", "--configuration" }, "The configuration to use for analysing the project. The default for most projects is 'Debug'."))
                .AddOption(new Option<string?>("--platform", "The platform to use for analysing the project. The default for most projects is 'AnyCPU'."))
                .AddOption(new Option<string?>(new string[] { "-s", "--source" }, "Specifies the server URL.").WithArgumentName("SOURCE").WithArity(ArgumentArity.OneOrMore))
                .AddOption(new Option<bool>("--no-version-suffix", "Forces there to be no version suffix. This overrides --version-suffix"))
                .AddOption(new Option<string?>("--version-suffix", "Sets the pre-release value. If none is specified, the pre-release from the previous version is used.").WithArgumentName("VERSION_SUFFIX"))
                .AddOption(new Option<bool>("--no-cache", "Disable using the machine cache as the first package source."))
                .AddOption(new Option<bool>("--direct-download", "Download directly without populating any caches with metadata or binaries."))
                .AddOption(new Option<string?>("--package-id-regex", "The regular expression to match in the package id.").WithArgumentName("REGEX"))
                .AddOption(new Option<string>("--package-id-replace", "The text used to replace the match from --package-id-regex").WithArgumentName("VALUE"))
                .AddOption(new Option<string?>("--package-id", "The package ID to check for previous versions").WithArgumentName("PACKAGE_ID").WithArity(ArgumentArity.OneOrMore))
                .AddOption(new Option<string?>("--exclude", "A package ID to check exclude from analysis").WithArgumentName("PACKAGE_ID").WithArity(ArgumentArity.OneOrMore));

            var diffCommandBuilder = new CommandBuilder(new Command("diff", "Calculates the differences"))
                .AddCommand(fileCommandBuilder.Command)
                .AddCommand(solutionCommandBuilder.Command)
                .AddGlobalOption(new Option<NuGet.Versioning.SemanticVersion?>(new string[] { "-p", "--previous" }, "The previous version") { Argument = new Argument<NuGet.Versioning.SemanticVersion>(argumentResult => NuGet.Versioning.SemanticVersion.Parse(argumentResult.Tokens.Single().Value)) }.WithDefaultValue(null))
                .AddGlobalOption(new Option<string>("--build-number-parameter", "The parameter name for the build number").WithArgumentName("PARAMETER").WithDefaultValue("buildNumber"))
                .AddGlobalOption(new Option<string>("--version-suffix-parameter", "The parameter name for the version suffix").WithArgumentName("PARAMETER").WithDefaultValue("system.build.suffix"))
                .AddGlobalOption(new Option<OutputTypes>("--output", "The output type").WithArgumentName("OUTPUT_TYPE").WithDefaultValue(OutputTypes.TeamCity | OutputTypes.Diagnostic))
                .AddGlobalOption(new Option<bool>(new string[] { "/nologo", "--nologo" }, "Do not display the startup banner or the copyright message."));

            var commandLineBuilder = new CommandLineBuilder(new RootCommand(description: "Semantic Version generator"))
                .UseDefaults()
                .AddCommand(diffCommandBuilder.Command);

            return commandLineBuilder
                .Build()
                .InvokeAsync(args);

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
                        ? (System.IO.FileSystemInfo)new System.IO.DirectoryInfo(path)
                        : new System.IO.FileInfo(path);
                }

                argumentResult.ErrorMessage = $"\"{pathToken}\" is not a valid file or directory";
                return default;
            }
        }

        private static Option<T> WithArgumentName<T>(this Option<T> option, string name)
        {
            option.Argument.Name = name;
            return option;
        }

        private static Option<T> WithDefaultValue<T>(this Option<T> option, T value)
        {
            option.Argument.SetDefaultValue(value);
            return option;
        }

        private static Option<T> WithArity<T>(this Option<T> option, IArgumentArity arity)
        {
            option.Argument.Arity = arity;
            return option;
        }
    }
}