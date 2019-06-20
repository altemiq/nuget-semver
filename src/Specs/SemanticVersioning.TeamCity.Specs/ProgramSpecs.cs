using FluentAssertions;
using Machine.Specifications;

namespace Altemiq.SemanticVersioning.TeamCity
{
    internal abstract class When_running_the_program
    {
        private static readonly object lockObject = new object();

        protected static System.Reflection.MethodInfo MainMethod;

        protected static (int, System.Collections.Generic.IEnumerable<string>) Invoke(params string[] args)
        {
            var consoleWriter = new ConsoleWriter(System.Console.Out);
            System.Console.SetOut(consoleWriter);
            var returnObject = MainMethod.Invoke(null, new object[] { args });

            (int, System.Collections.Generic.IEnumerable<string>) returnValue;
            switch (returnObject)
            {
                case System.Threading.Tasks.Task<int> intTask:
                    returnValue = (intTask.Result, consoleWriter.CapturedOutput);
                    break;
                case System.Threading.Tasks.Task task:
                    task.Wait();
                    returnValue = (0, consoleWriter.CapturedOutput);
                    break;
                case int intValue:
                    returnValue = (intValue, consoleWriter.CapturedOutput);
                    break;
                default:
                    return (0, consoleWriter.CapturedOutput);
            }

            return returnValue;
        }

        protected static string GetProjectPath(string project)
        {
            var projectDirectory = GetProjectDirectory(project);
            var projectName = System.IO.Path.GetFileName(projectDirectory);
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDirectory, projectName + ".csproj"));
        }

        protected static string GetProjectDirectory(string project) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetTestDirectory(), "..", project));

        protected static string GetSource(string source) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetTestDirectory(), "..", "NuPkg", source));

        private static string GetTestDirectory()
        {
            var currentPath = System.IO.Path.GetDirectoryName(typeof(When_running_the_program).Assembly.Location);
            return System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(currentPath)));
        }

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

            public ConsoleWriter(System.IO.TextWriter consoleOut) => this.Out = consoleOut;

            public System.Collections.Generic.IEnumerable<string> CapturedOutput => this.stringBuilder.ToString().Split(System.Environment.NewLine);

            private readonly System.IO.TextWriter Out;

            public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

            public override void Write(string value)
            {
                this.stringBuilder.Append(value);
                this.Out.Write(value);
            }

            public override void WriteLine(string value)
            {
                this.stringBuilder.AppendLine(value);
                this.Out.WriteLine(value);
            }
        }
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_no_valid_nuget : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("NoPackages"), "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[system.build.number '0.1.0']");

        private readonly It should_have_returned_a_blank_suffix = () => returnValue.console.Should().Contain("##teamcity[system.build.suffix '']");
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_no_full_release : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("OnlyPrerelease"), "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[system.build.number '1.0.2']");

        private readonly It should_have_returned_a_suffix = () => returnValue.console.Should().Contain("##teamcity[system.build.suffix 'develop']");
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_no_pre_release : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("OnlyRelease"), "--direct-download", "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[system.build.number '1.0.1']");

        private readonly It should_have_returned_a_blank_suffix = () => returnValue.console.Should().Contain("##teamcity[system.build.suffix '']");
    }

    [Subject(typeof(Program))]
    internal class When_running_the_program_with_pre_release_and_release : When_running_the_program
    {
        private static (int exitValue, System.Collections.Generic.IEnumerable<string> console) returnValue;

        private readonly Because of = () => returnValue = Invoke("diff", "solution", GetProjectPath("Projects\\Original"), "--source", GetSource("Full"), "--direct-download", "--no-cache");

        private readonly It should_return_a_success_exit_value = () => returnValue.exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => returnValue.console.Should().Contain("##teamcity[system.build.number '1.0.2']");

        private readonly It should_have_returned_a_blank_suffix = () => returnValue.console.Should().Contain("##teamcity[system.build.suffix 'develop']");
    }
}
