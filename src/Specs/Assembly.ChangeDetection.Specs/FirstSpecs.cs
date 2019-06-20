namespace Altemiq.Assembly.ChangeDetection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using FluentAssertions;
    using Machine.Specifications;

    internal abstract class When_there_are_changes
    {
        protected static readonly string BaseAssembly = GetPath("Projects\\Original", "Original.dll");

        protected static string GetPath(string project, string name)
        {
            var currentPath = System.IO.Path.GetDirectoryName(typeof(When_there_are_changes).Assembly.Location);

            var framework = System.IO.Path.GetFileName(currentPath);
            currentPath = System.IO.Path.GetDirectoryName(currentPath);

            var configuration = System.IO.Path.GetFileName(currentPath);
            currentPath = System.IO.Path.GetDirectoryName(currentPath);

            var type = System.IO.Path.GetFileName(currentPath);
            var testProjectDirectory = System.IO.Path.GetDirectoryName(currentPath);

            var projectDirectory = System.IO.Path.Combine(testProjectDirectory, "..", project, type, configuration);

            framework = System.IO.Directory.EnumerateDirectories(projectDirectory).First();

            return System.IO.Path.GetFullPath(System.IO.Path.Combine(framework, name));
        }
    }

    [Subject(typeof(SemVer.SemanticVersionAnalyzer))]
    internal class When_there_are_breaking_changes : When_there_are_changes
    {
        private static readonly string TestAssembly = GetPath("Projects\\BreakingChange", "Original.dll");

        private static SemVer.AnalysisResult analysisResult;

        private readonly Establish context = () => { };

        private readonly Because of = () => analysisResult = SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, TestAssembly, "1.0.1-alpha");

        private readonly It should_have_a_major_version_change = () => analysisResult.ResultsType.Should().Be(SemVer.ResultsType.Major);

        private readonly It should_have_a_version_of_two = () => analysisResult.VersionNumber.Should().Be("2.0.0-alpha");
    }

    [Subject(typeof(SemVer.SemanticVersionAnalyzer))]
    internal class When_there_are_non_breaking_changes : When_there_are_changes
    {
        private static readonly string TestAssembly = GetPath("Projects\\NonBreakingAdditiveChange", "Original.dll");

        private static SemVer.AnalysisResult analysisResult;

        private readonly Establish context = () => { };

        private readonly Because of = () => analysisResult = SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, TestAssembly, "1.0.1");

        private readonly It should_have_a_minor_version_change = () => analysisResult.ResultsType.Should().Be(SemVer.ResultsType.Minor);

        private readonly It should_have_a_version_of_one_point_one = () => analysisResult.VersionNumber.Should().Be("1.1.0");
    }

    [Subject(typeof(SemVer.SemanticVersionAnalyzer))]
    internal class When_there_are_no_changes : When_there_are_changes
    {
        private static readonly string TestAssembly = GetPath("Projects\\Original", "Original.dll");

        private static SemVer.AnalysisResult analysisResult;

        private readonly Establish context = () => { };

        private readonly Because of = () => analysisResult = SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, TestAssembly, "1.0.1-beta");

        private readonly It should_have_a_patch_version_change = () => analysisResult.ResultsType.Should().Be(SemVer.ResultsType.Patch);

        private readonly It should_have_a_version_of_one_point_zero_point_two = () => analysisResult.VersionNumber.Should().Be("1.0.2-beta");
    }
}
