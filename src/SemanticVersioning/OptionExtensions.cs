namespace Altemiq.SemanticVersioning
{
    using System.CommandLine;

    internal static class OptionExtensions
    {

        public static Option<T> WithArgumentName<T>(this Option<T> option, string name)
        {
            option.Argument.Name = name;
            return option;
        }

        public static Option<T> WithDefaultValue<T>(this Option<T> option, T value)
        {
            option.Argument.SetDefaultValue(value);
            return option;
        }

        public static Option<T> WithArity<T>(this Option<T> option, IArgumentArity arity)
        {
            option.Argument.Arity = arity;
            return option;
        }
    }
}
