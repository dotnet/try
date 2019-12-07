// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Server
{
    public abstract class KernelCommandEnvelope : IKernelCommandEnvelope
    {
        private static readonly ConcurrentDictionary<Type, Func<IKernelCommand, IKernelCommandEnvelope>> _envelopeFactories =
            new ConcurrentDictionary<Type, Func<IKernelCommand, IKernelCommandEnvelope>>();

        private readonly IKernelCommand _command;

        private static readonly JsonSerializerOptions _deserializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new ConstructorInjectingJsonConverter()
            }
        };

        private static readonly JsonSerializerOptions _serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        

        protected KernelCommandEnvelope(IKernelCommand command)
        {
            _command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public string Token { get; }

        public abstract string CommandType { get; }

        IKernelCommand IKernelCommandEnvelope.Command => _command;

        private static readonly Dictionary<string, Type> _commandEnvelopeTypesByCommandTypeName = new Dictionary<string, Type>
        {
            ["AddNugetPackage"] = typeof(KernelCommandEnvelope<AddNugetPackage>),
            ["CancelCurrentCommand"] = typeof(KernelCommandEnvelope<CancelCurrentCommand>),
            ["DisplayError"] = typeof(KernelCommandEnvelope<DisplayError>),
            ["DisplayValue"] = typeof(KernelCommandEnvelope<DisplayValue>),
            ["LoadExtension"] = typeof(KernelCommandEnvelope<LoadExtension>),
            ["LoadExtensionsInDirectory"] = typeof(KernelCommandEnvelope<LoadExtensionsInDirectory>),
            ["Quit"] = typeof(KernelCommandEnvelope<Quit>),
            ["RequestCompletion"] = typeof(KernelCommandEnvelope<RequestCompletion>),
            ["RequestDiagnostics"] = typeof(KernelCommandEnvelope<RequestDiagnostics>),
            ["RunDirective"] = typeof(KernelCommandEnvelope<RunDirective>),
            ["SubmitCode"] = typeof(KernelCommandEnvelope<SubmitCode>),
            ["UpdateDisplayedValue"] = typeof(KernelCommandEnvelope<UpdateDisplayedValue>)
        };

        public static string Serialize(IKernelCommandEnvelope envelope)
        {
            return JsonSerializer.Serialize(
                envelope,
                envelope.GetType(),
                _serializeOptions);
        }

        public static IKernelCommandEnvelope Deserialize(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);

            var commandType = document.RootElement.GetProperty("commandType").GetString();

            var envelopeType = _commandEnvelopeTypesByCommandTypeName[commandType];

            return (IKernelCommandEnvelope) JsonSerializer.Deserialize(
                json,
                envelopeType,
                _deserializeOptions);
        }

        public static IKernelCommandEnvelope Create(IKernelCommand command)
        {
            var factory = _envelopeFactories.GetOrAdd(
                command.GetType(),
                commandType =>
                {
                    var genericType = _commandEnvelopeTypesByCommandTypeName[command.GetType().Name];

                    var constructor = genericType.GetConstructors().Single();

                    var commandParameter = Expression.Parameter(
                        typeof(IKernelCommand),
                        "c");

                    var newExpression = Expression.New(
                        constructor,
                        Expression.Convert(commandParameter, commandType));

                    var expression = Expression.Lambda<Func<IKernelCommand, IKernelCommandEnvelope>>(
                        newExpression,
                        commandParameter);

                    return expression.Compile();
                });

            return factory(command);
        }
    }
}