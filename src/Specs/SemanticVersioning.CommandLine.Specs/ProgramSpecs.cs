using FluentAssertions;
using Machine.Specifications;

namespace Altemiq.SemanticVersioning
{
    internal abstract class When_running_the_program
    {
        private static readonly object lockObject = new();

        protected static System.Reflection.MethodInfo MainMethod;

        protected static (int Return, System.Collections.Generic.IEnumerable<string> Output, System.Collections.Generic.IEnumerable<string> Error) Invoke(params string[] args)
        {
            var consoleOut = new ConsoleWriter(System.Console.Out);
            System.Console.SetOut(consoleOut);

            var consoleError = new ConsoleWriter(System.Console.Error);
            System.Console.SetError(consoleError);

            var returnObject = MainMethod.Invoke(null, new object[] { args });
            var (exitValue, console, error) = returnObject switch
            {
                System.Threading.Tasks.Task<int> intTask => Return(intTask.Result),
                System.Threading.Tasks.Task task => WaitThenReturn(task),
                int intValue => Return(intValue),
                _ => Return(),
            };

            System.Console.SetOut(consoleOut.Original);
            System.Console.SetError(consoleError.Original);

            return (exitValue, console, error);

            (int Return, System.Collections.Generic.IEnumerable<string> Output, System.Collections.Generic.IEnumerable<string> Error) WaitThenReturn(System.Threading.Tasks.Task task, int value = 0)
            {
                task.Wait();
                return Return(value);
            }

            (int Return, System.Collections.Generic.IEnumerable<string> Output, System.Collections.Generic.IEnumerable<string> Error) Return(int value = 0)
            {
                return (value, consoleOut.CapturedOutput, consoleError.CapturedOutput);
            }
        }

        protected static string GetProjectPath(string project) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetProjectDirectory(project), System.IO.Path.GetFileName(project) + ".csproj"));

        protected static string GetProjectDirectory(string project) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetTestDirectory(), "..", project));

        protected static string GetSource(string source) => System.IO.Path.GetFullPath(System.IO.Path.Combine(GetTestDirectory(), "..", "NuPkg", source));

        private static string GetTestDirectory() => System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(typeof(When_running_the_program).Assembly.Location))));

        private readonly Establish context = () =>
        {
            lock (lockObject)
            {
                if (MainMethod is null)
                {
                    MainMethod = EntryPointDiscoverer.FindStaticEntryMethod(typeof(ConsoleApplication).Assembly);
                }
            }
        };

        private class ConsoleWriter : System.IO.TextWriter
        {
            private readonly System.Text.StringBuilder stringBuilder = new();

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

    [Subject(typeof(MSBuildApplication))]
    internal class When_running_the_program_with_no_valid_nuget : When_running_the_program
    {
        private static int exitValue;
        private static System.Collections.Generic.IEnumerable<string> console;
        private static  System.Collections.Generic.IEnumerable<string> error;

        private readonly Because of = () => (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(System.IO.Path.Combine("Projects", "Original")), "--source", GetSource("NoPackages"), "--no-cache");

        private readonly It should_return_a_success_exit_value = () => exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => console.Should().Contain("##teamcity[buildNumber '1.0.0']");

        private readonly It should_have_returned_a_blank_suffix = () => console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='alpha']");

        private readonly It should_not_have_thrown_an_exception = () => error.Should().BeEmpty();
    }

    internal class When_running_the_program_with_a_name_change : When_running_the_program
    {
        private static int exitValue;
        private static System.Collections.Generic.IEnumerable<string> console;
        private static System.Collections.Generic.IEnumerable<string> error;

        private readonly Because of = () => (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(System.IO.Path.Combine("Projects" ,"New")), "--source", GetSource("OnlyRelease"), "--no-cache", "--package-id-regex", "New", "--package-id-replace", "Original");

        private readonly It should_return_a_success_exit_value = () => exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => console.Should().Contain("##teamcity[buildNumber '2.0.0']");

        private readonly It should_have_returned_a_blank_suffix = () => console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='alpha']");

        private readonly It should_not_have_thrown_an_exception = () => error.Should().BeEmpty();
    }

    [Subject(typeof(MSBuildApplication))]
    internal class When_running_the_program_with_no_full_release : When_running_the_program
    {
        private static int exitValue;
        private static System.Collections.Generic.IEnumerable<string> console;
        private static System.Collections.Generic.IEnumerable<string> error;

        private readonly Because of = () => (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(System.IO.Path.Combine("Projects", "Original")), "--source", GetSource("OnlyPrerelease"), "--no-cache");

        private readonly It should_return_a_success_exit_value = () => exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => console.Should().Contain("##teamcity[buildNumber '1.0.2']");

        private readonly It should_have_returned_a_suffix = () => console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='develop']");

        private readonly It should_not_have_thrown_an_exception = () => error.Should().BeEmpty();
    }

    [Subject(typeof(MSBuildApplication))]
    internal class When_running_the_program_with_no_pre_release : When_running_the_program
    {
        private static int exitValue;
        private static System.Collections.Generic.IEnumerable<string> console;
        private static System.Collections.Generic.IEnumerable<string> error;

        private readonly Because of = () => (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(System.IO.Path.Combine("Projects", "Original")), "--source", GetSource("OnlyRelease"), "--direct-download", "--no-cache");

        private readonly It should_return_a_success_exit_value = () => exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => console.Should().Contain("##teamcity[buildNumber '1.0.1']");

        private readonly It should_have_returned_a_blank_suffix = () => console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='']");

        private readonly It should_not_have_thrown_an_exception = () => error.Should().BeEmpty();
    }

    [Subject(typeof(MSBuildApplication))]
    internal class When_running_the_program_with_pre_release_and_release : When_running_the_program
    {
        private static int exitValue;
        private static System.Collections.Generic.IEnumerable<string> console;
        private static System.Collections.Generic.IEnumerable<string> error;

        private readonly Because of = () => (exitValue, console, error) = Invoke("diff", "solution", GetProjectPath(System.IO.Path.Combine("Projects", "Original")), "--source", GetSource("Full"), "--direct-download", "--no-cache");

        private readonly It should_return_a_success_exit_value = () => exitValue.Should().Be(0);

        private readonly It should_have_returned_a_version = () => console.Should().Contain("##teamcity[buildNumber '1.0.2']");

        private readonly It should_have_returned_a_blank_suffix = () => console.Should().Contain("##teamcity[setParameter name='system.build.suffix' value='develop']");

        private readonly It should_not_have_thrown_an_exception = () => error.Should().BeEmpty();
    }
}
