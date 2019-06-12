// code adapted from https://github.com/dotnet/command-line-api/blob/166610c56ff732093f0145a2911d4f6c40b786da/src/System.CommandLine.DragonFruit/CommandLine.cs

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace MLS.WasmCodeRunner
{
    internal static class CommandLineBuilderExtensions
    {
        public static CommandLineBuilder ConfigureRootCommandFromMethod(
            this CommandLineBuilder builder,
            MethodInfo method)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            builder.Command.ConfigureFromMethod(method);

            return builder;
        }

        private static readonly string[] _argumentParameterNames =
        {
            "arguments",
            "argument",
            "args"
        };

        public static void ConfigureFromMethod(
            this Command command,
            MethodInfo method)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            foreach (var option in method.BuildOptions())
            {
                command.AddOption(option);
            }

            if (method.GetParameters()
                .FirstOrDefault(p => _argumentParameterNames.Contains(p.Name)) is ParameterInfo argsParam)
            {
                command.AddArgument(new Argument
                {
                    ArgumentType = argsParam.ParameterType,
                    Name = argsParam.Name
                });
            }

            command.Handler = CommandHandler.Create(method);
        }

        public static IEnumerable<Option> BuildOptions(this MethodInfo type)
        {
            var descriptor = HandlerDescriptor.FromMethodInfo(type);

            var omittedTypes = new[]
            {
                typeof(IConsole),
                typeof(InvocationContext),
                typeof(BindingContext),
                typeof(ParseResult),
                typeof(CancellationToken),
            };

            foreach (var option in descriptor.ParameterDescriptors
                .Where(d => !omittedTypes.Contains(d.Type))
                .Where(d => !_argumentParameterNames.Contains(d.ValueName))
                .Select(p => p.BuildOption()))
            {
                yield return option;
            }
        }

        public static Option BuildOption(this ParameterDescriptor parameter)
        {
            var argument = new Argument
            {
                ArgumentType = parameter.Type
            };

            if (parameter.HasDefaultValue)
            {
                argument.SetDefaultValue(parameter.GetDefaultValue);
            }

            var option = new Option(
                parameter.BuildAlias(),
                parameter.ValueName,
                argument);

            return option;
        }

        public static string BuildAlias(this IValueDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return BuildAlias(descriptor.ValueName);
        }

        internal static string BuildAlias(string parameterName)
        {
            if (String.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(parameterName));
            }

            return parameterName.Length > 1
                ? $"--{parameterName.ToKebabCase()}"
                : $"-{parameterName.ToLowerInvariant()}";
        }
    }
}