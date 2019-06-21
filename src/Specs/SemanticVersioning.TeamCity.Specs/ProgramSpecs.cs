using FluentAssertions;
using Machine.Specifications;

namespace Mondo.SemanticVersioning.TeamCity
{
    internal abstract class When_running_the_program
    {
        private static readonly object lockObject = new object();

        protected static System.Reflection.MethodInfo MainMethod;

        protected static (int, System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IEnumerable<string>) Invoke(params string[] args)
        {
            var consoleOut = new ConsoleWriter(System.Console.Out);
            System.Console.SetOut(consoleOut);

            var consoleError = new ConsoleWriter(System.Console.Error);
            System.Console.SetError(consoleError);

            var returnObject = MainMethod.Invoke(null, new object[] { args });

            (int, System.Collections.Generic.IEnumerable<string>, System.Collections.Generic.IEnumerable<string>) returnValue;
            switch (returnObject)
            {
                case System.Threading.Tasks.Task<int> intTask:
                    returnValue = (intTask.Result, consoleOut.CapturedOutput, consoleError.CapturedOutput);
                    break;
                case System.Threading.Tasks.Task task:
                    task.Wait();
                    returnValue = (0, consoleOut.CapturedOutput, consoleError.CapturedOutput);
                    break;
                case int intValue:
                    returnValue = (intValue, consoleOut.CapturedOutput, consoleError.CapturedOutput);
                    break;
                default:
                    returnValue = (0, consoleOut.CapturedOutput, consoleError.CapturedOutput);
                    break;
            }

            System.Console.SetOut(consoleOut.Original);
            System.Console.SetError(consoleError.Original);

            return returnValue;
        }

        protected static string GetProjectPath(string project) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetProjectDirectory(project), System.IO.Path.GetFileName(project) + ".csproj"));

        protected static string GetProjectDirectory(string project) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetTestDirectory(), "..", project));

        protected static string GetSource(string source) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetTestDirectory(), "..", "NuPkg", source));

        private static string GetTestDirectory() => System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(typeof(When_running_the_program).Assembly.Location))));

        private readonly Establish context = () =>
        {
            lock (lockObject)
            {
                if (MainMethod == null)
                {
                    MainMethod = EntryPointDiscoverer.FindStaticEntryMethod(typeof(Program).Assembly);
                }
            }
        };

        private class ConsoleWriter : System.IO.TextWriter
        {
            private readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

            public ConsoleWriter(System.IO.TextWriter original) => this.Original = original;

            public System.Collections.Generic.IEnumerable<string> CapturedOutput => this.stringBuilder.Length == 0
                ? System.Linq.Enumerable.Empty<string>()
                : this.stringBuilder.ToString().Split(System.Environment.NewLine);

            public System.IO.TextWriter Original { get; }

            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            public override void Write(string value) => this.stringBuilder.Append(value);

            public override void WriteLine(string value) => this.stringBuilder.AppendLine(value);
        }
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_no_valid_nuget : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console, System.Collections.Generic.IEnumerable<string> error) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("NoPackages"), "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[buildNumber '1.0.0']");

        private readonly It should_have_returned_a_blank_suffix = () => returnValue.console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='alpha']");

        private readonly It should_not_have_thrown_an_exception = () => returnValue.error.Should().BeEmpty();
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_no_full_release : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console, System.Collections.Generic.IEnumerable<string> error) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("OnlyPrerelease"), "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[buildNumber '1.0.2']");

        private readonly It should_have_returned_a_suffix = () => returnValue.console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='develop']");

        private readonly It should_not_have_thrown_an_exception = () => returnValue.error.Should().BeEmpty();
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_no_pre_release : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console, System.Collections.Generic.IEnumerable<string> error) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("OnlyRelease"), "--direct-download", "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[buildNumber '1.0.1']");

        private readonly It should_have_returned_a_blank_suffix = () => returnValue.console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='']");

        private readonly It should_not_have_thrown_an_exception = () => returnValue.error.Should().BeEmpty();
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_pre_release_and_release : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console, System.Collections.Generic.IEnumerable<string> error) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("Full"), "--direct-download", "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[buildNumber '1.0.2']");

        private readonly It should_have_returned_a_blank_suffix = () => returnValue.console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='develop']");

        private readonly It should_not_have_thrown_an_exception = () => returnValue.error.Should().BeEmpty();
    }
}
