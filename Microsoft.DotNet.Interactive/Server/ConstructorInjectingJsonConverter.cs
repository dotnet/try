// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Server
{
    internal class ConstructorInjectingJsonConverter : JsonConverter<object>
    {
        private readonly ConcurrentDictionary<Type, Func<JsonElement, object>> _deserializers = new ConcurrentDictionary<Type, Func<JsonElement, object>>();

        private static readonly HashSet<string> _supportedNamespaces = new HashSet<string>
        {
            typeof(IKernel).Namespace,
            typeof(IKernelCommand).Namespace,
            typeof(IKernelCommandEnvelope).Namespace,
            typeof(IKernelEvent).Namespace,
        };

        public override object Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            using var jsonDocument = JsonDocument.ParseValue(ref reader);

            return Deserialize(
                jsonDocument.RootElement,
                typeToConvert,
                options);
        }

        private object Deserialize(
            JsonElement jsonElement,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var deserializer = _deserializers.GetOrAdd(typeToConvert, t =>
            {
                var ctors = typeToConvert.GetConstructors();
                var longestCtorParamCount = ctors.Any()
                                                ? ctors.Max(c => c.GetParameters().Length)
                                                : 0;

                var chosenCtor = ctors.Single(c => c.GetParameters().Length == longestCtorParamCount);

                var jsonElementParameter = Expression.Parameter(typeof(JsonElement), "jsonElement");

                var ctorParameters = chosenCtor
                                     .GetParameters()
                                     .Select(p => ParseParameter(p, jsonElementParameter))
                                     .ToArray();

                var newExpression = Expression.New(
                    chosenCtor,
                    ctorParameters);

                var factoryExpr = Expression.Lambda<Func<JsonElement, object>>(
                    newExpression,
                    jsonElementParameter);

                var factory = factoryExpr.Compile();

                return factory;
            });

            return deserializer(jsonElement);

            static Expression ParseParameter(ParameterInfo p, ParameterExpression jsonElementParameter)
            {
                if (p.HasDefaultValue)
                {
                    if (IsNullable(p))
                    {
                        return CreateNullable(p);
                    }
                    else
                    {
                        if (IsStructWithNoDefaultValue(p))
                        {
                            return CreateDefaultStruct(p);
                        }
                        else
                        {
                            if (IsPrimitiveWithDefaultValue(p))
                            {
                                return UseSpecifiedDefaultValue(p);
                            }
                            else
                            {
                                return GetOptionalParameterValue(
                                    jsonElementParameter,
                                    p.Name,
                                    p.ParameterType);
                            }
                        }
                    }
                }
                else
                {
                    return GetParameterValue(
                        jsonElementParameter,
                        p.Name,
                        p.ParameterType);
                }
            }

            bool IsStructWithNoDefaultValue(ParameterInfo p) =>
                p.ParameterType.IsValueType && Equals(p.DefaultValue, null);

            bool IsNullable(ParameterInfo p) =>
                p.ParameterType.IsGenericType &&
                p.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>);

            bool IsPrimitiveWithDefaultValue(ParameterInfo p) =>
                p.HasDefaultValue && (p.ParameterType.IsPrimitive || p.ParameterType == typeof(string));

            Expression CreateDefaultStruct(ParameterInfo p) =>
                Expression.Constant(Activator.CreateInstance(p.ParameterType), p.ParameterType);

            ConstantExpression UseSpecifiedDefaultValue(ParameterInfo p) =>
                Expression.Constant(p.DefaultValue, p.ParameterType);

            Expression CreateNullable(ParameterInfo p)
            {
                var genericArgument = p.ParameterType.GetGenericArguments()[0];

                var defaultValueForType = Activator.CreateInstance(genericArgument);

                var argument = Expression.Constant(
                    p.DefaultValue ?? defaultValueForType);

                var constructor = typeof(Nullable<>)
                                  .MakeGenericType(genericArgument)
                                  .GetConstructor(new[] { genericArgument });

                return Expression.New(constructor, argument);
            }

            Expression GetParameterValue(
                ParameterExpression parentJsonElement,
                string propertyName,
                Type type)
            {
                MethodInfo getProperty = typeof(JsonElement).GetMethod("GetProperty", new[] { typeof(string) });

                var jsonProperty = Expression.Call(
                    parentJsonElement,
                    getProperty,
                    Expression.Constant(propertyName));

                var getRawText = typeof(JsonElement).GetMethod("GetRawText");

                var rawTextParameter = Expression.Call(
                    jsonProperty,
                    getRawText);

                var deserializeMethod = typeof(JsonSerializer)
                    .GetMethod("Deserialize",
                               new[]
                               {
                                   typeof(string),
                                   typeof(Type),
                                   typeof(JsonSerializerOptions)
                               });

                var deserialize =
                    Expression.Convert(
                        Expression.Call(
                            null,
                            deserializeMethod,
                            new Expression[]
                            {
                                rawTextParameter,
                                Expression.Constant(type),
                                Expression.Constant(options)
                            }
                        ), type);

                return deserialize;
            }

            Expression GetOptionalParameterValue(
                ParameterExpression parentJsonElement,
                string propertyName,
                Type type)
            {
                return GetParameterValue(
                    parentJsonElement,
                    propertyName,
                    type);
            }
        }

        public override void Write(
            Utf8JsonWriter writer,
            object value,
            JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type typeToConvert) =>
            _supportedNamespaces.Contains(typeToConvert.Namespace);
    }
}