namespace Altemiq.Assembly.ChangeDetection
{
    using System.Linq;
    using FluentAssertions;
    using Machine.Specifications;

    internal abstract class When_there_are_changes
    {
        protected static readonly string BaseAssembly = GetPath("Projects\\Original", "Original.dll");

        protected static string GetPath(string project, string name)
        {
            var currentPath = System.IO.Path.GetDirectoryName(typeof(When_there_are_changes).Assembly.Location);

            currentPath = System.IO.Path.GetDirectoryName(currentPath);

            var configuration = System.IO.Path.GetFileName(currentPath);
            currentPath = System.IO.Path.GetDirectoryName(currentPath);

            var type = System.IO.Path.GetFileName(currentPath);
            var testProjectDirectory = System.IO.Path.GetDirectoryName(currentPath);

            var projectDirectory = System.IO.Path.Combine(testProjectDirectory, "..", project, type, configuration);

            var framework = System.IO.Directory.EnumerateDirectories(projectDirectory).First();

            return System.IO.Path.GetFullPath(System.IO.Path.Combine(framework, name)).Replace('\\', System.IO.Path.DirectorySeparatorChar);
        }
    }

    [Subject(typeof(SemVer.SemanticVersionAnalyzer))]
    internal class When_there_are_breaking_changes : When_there_are_changes
    {
        private static readonly string TestAssembly = GetPath("Projects\\BreakingChange", "Original.dll");

        private static SemVer.AnalysisResult analysisResult;

        private readonly Establish context = () => { };

        private readonly Because of = () => analysisResult = SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, TestAssembly, new[] { "1.0.1-alpha" });

        private readonly It should_have_a_major_version_change = () => analysisResult.ResultsType.Should().Be(SemVer.ResultsType.Major);

        private readonly It should_have_a_version_of_two = () => analysisResult.VersionNumber.Should().Be("2.0.0-alpha");
    }

    [Subject(typeof(SemVer.SemanticVersionAnalyzer))]
    internal class When_there_are_non_breaking_changes : When_there_are_changes
    {
        private static readonly string TestAssembly = GetPath("Projects\\NonBreakingAdditiveChange", "Original.dll");

        private static SemVer.AnalysisResult analysisResult;

        private readonly Establish context = () => { };

        private readonly Because of = () => analysisResult = SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, TestAssembly, new[] { "1.0.1" });

        private readonly It should_have_a_minor_version_change = () => analysisResult.ResultsType.Should().Be(SemVer.ResultsType.Minor);

        private readonly It should_have_a_version_of_one_point_one = () => analysisResult.VersionNumber.Should().Be("1.1.0-alpha");
    }

    [Subject(typeof(SemVer.SemanticVersionAnalyzer))]
    internal class When_there_are_no_changes : When_there_are_changes
    {
        private static readonly string TestAssembly = GetPath("Projects\\Original", "Original.dll");

        private static SemVer.AnalysisResult analysisResult;

        private readonly Establish context = () => { };

        private readonly Because of = () => analysisResult = SemVer.SemanticVersionAnalyzer.Analyze(BaseAssembly, TestAssembly, new[] { "1.0.1-beta" });

        private readonly It should_have_a_patch_version_change = () => analysisResult.ResultsType.Should().Be(SemVer.ResultsType.Patch);

        private readonly It should_have_a_version_of_one_point_zero_point_two = () => analysisResult.VersionNumber.Should().Be("1.0.2-beta");
    }

    [Subject(typeof(SemVer.SemanticVersionAnalyzer))]
    internal class When_there_is_an_name_change : When_there_are_changes
    {
        private static readonly string TestAssembly = GetPath("Projects\\New", "New.dll");

        private static SemVer.AnalysisResult analysisResult;

        private readonly Establish context = () => { };

        private readonly Because of = () => analysisResult = SemVer.SemanticVersionAnalyzer.Analyze(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(BaseAssembly), System.IO.Path.GetFileName(TestAssembly)), TestAssembly, new[] { "1.0.1" });

        private readonly It should_have_a_major_version_change = () => analysisResult.ResultsType.Should().Be(SemVer.ResultsType.Major);

        private readonly It should_have_a_version_of_two_point_zero_point_zero_dash_alpha = () => analysisResult.VersionNumber.Should().Be("2.0.0-alpha");
    }
}
