// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.TryDotNet.PeakyTests;

internal static partial class EnvironmentVariableDeserializer
{
    public static T DeserializeFromEnvVars<T>(IEnvironmentVariableAccess envAccess = null) where T : class
    {
        return DeserializeFromEnvVars(typeof(T), envAccess) as T;
    }

    public static object DeserializeFromEnvVars(Type type, IEnvironmentVariableAccess envAccess = null)
    {
        envAccess = envAccess ?? new RealEnvironmentVariableAccess();

        EnsureValidSettingsType(type);

        var properties = GetPropertyDeserializationInfoFor(type, envAccess);

        EnsureAllDeserialized(properties);

        return GetInstanceOf(type, properties);
    }

    private static void EnsureValidSettingsType(Type type)
    {
        if (!type.HasDefaultConstructor())
        {
            throw new EnvironmentVariableDeserializationException(type);
        }
    }

    public class EnvironmentVariableDeserializationException : Exception
    {
        internal EnvironmentVariableDeserializationException(Type type) :
            base(GetExceptionMessage(type ?? throw new ArgumentNullException(nameof(type))))
        {
        }

        internal EnvironmentVariableDeserializationException(PropertyInfo propertyInfo) :
            base(GetExceptionMessage(propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo))))
        {
        }

        internal EnvironmentVariableDeserializationException(List<PropertyDeserializationInfo> properties) :
            base(GetExceptionMessage(properties ??
                                     throw new ArgumentNullException(nameof(properties))))
        {
        }

        private static string GetExceptionMessage(PropertyInfo propertyInfo)
        {
            return $"Environment Variable Parsing: No setter for {propertyInfo.Name}";
        }

        private static string GetExceptionMessage(IEnumerable<PropertyDeserializationInfo> properties)
        {
            var sb = new StringBuilder("Some properties could not be deserialized:");

            sb.AppendLine();

            foreach (var property in properties)
            {
                sb.Append($"- {property.PropertyName}: ");

                if (property.PropertyValue != null)
                {
                    sb.AppendLine("OK");
                }
                else if (property.EnvironmentVariableValue != null)
                {
                    sb.AppendLine($"Failed to deserialize as {property.PropertyInfo.PropertyType}");
                }
                else
                {
                    sb.AppendLine($"Environment Variable {property.EnvironmentVariableName} is not defined");
                }
            }

            return sb.ToString();
        }

        private static string GetExceptionMessage(Type type)
        {
            return $"No default constructor for {type.FullName}";
        }
    }

    private static object GetInstanceOf(Type type, List<PropertyDeserializationInfo> properties)
    {
        var constructor = type.GetDefaultConstructor();

        var instance = constructor.Invoke(new object[] { });

        foreach (var property in properties)
        {
            property.PropertyInfo.SetValue(instance, property.PropertyValue);
        }

        return instance;
    }

    private static void EnsureAllDeserialized(List<PropertyDeserializationInfo> properties)
    {
        if (properties.Any(p => p.PropertyValue == null))
        {
            throw new EnvironmentVariableDeserializationException(properties);
        }
    }

    private static List<PropertyDeserializationInfo> GetPropertyDeserializationInfoFor(Type type, IEnvironmentVariableAccess envAccess)
    {
        var propertyDeserializationInfos = new List<PropertyDeserializationInfo>();
        var typeName = GetTypeNameOf(type);

        foreach (var property in type.GetProperties(~BindingFlags.Static))
        {
            var propertyName = property.Name;
            var propertyShortNames = (ShortNameAttribute[])property.GetCustomAttributes(typeof(ShortNameAttribute), inherit: false);

            if (propertyShortNames.Length > 0)
            {
                propertyName = typeof(ShortNameAttribute).GetField("Name").GetValue(propertyShortNames[0]) as string;
            }

            if (property.GetSetMethod() is null)
            {
                throw new EnvironmentVariableDeserializationException(property);
            }

            var envVarName = $"CUSTOMCONNSTR_{typeName}_{propertyName}";
            var envVarValue = envAccess.Get(envVarName);

            propertyDeserializationInfos.Add(new PropertyDeserializationInfo
            {
                EnvironmentVariableName = envVarName,
                EnvironmentVariableValue = envVarValue,
                PropertyInfo = property,
                PropertyName = propertyName,
                PropertyValue = DeserializeProperty(property.PropertyType, envVarValue, envAccess)
            });
        }

        return propertyDeserializationInfos;
    }

    private static object DeserializeProperty(Type propertyType, string envVarValue, IEnvironmentVariableAccess envAccess)
    {
        if (propertyType == typeof(string))
        {
            return envVarValue;
        }

        if (propertyType == typeof(bool))
        {
            return bool.TryParse(envVarValue, out var boolValue)
                       ? (object)boolValue
                       : null;
        }

        if (propertyType == typeof(int))
        {
            return int.TryParse(envVarValue, out var intValue)
                       ? (object)intValue
                       : null;
        }

        if (propertyType.GenericTypeArguments.Length == 2 &&
            propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return JsonConvert.DeserializeObject(envVarValue, propertyType);
        }

        if (propertyType.IsClass)
        {
            return DeserializeFromEnvVars(propertyType, envAccess);
        }

        return null;
    }

    private static string GetTypeNameOf(Type type)
    {
        var typeName = type.Name;
        var shortNames = (ShortNameAttribute[])type.GetCustomAttributes(typeof(ShortNameAttribute), inherit: false);

        if (shortNames.Length > 0)
        {
            typeName = typeof(ShortNameAttribute).GetField("Name").GetValue(shortNames[0]) as string;
        }

        return typeName;
    }
}