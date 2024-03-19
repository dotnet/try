// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

using System.Reflection;

namespace Microsoft.TryDotNet.PeakyTests;

internal class PropertyDeserializationInfo
{
    public string PropertyName { get; set; }
    public PropertyInfo PropertyInfo { get; set; }
    public string EnvironmentVariableName { get; set; }
    public string EnvironmentVariableValue { get; set; }
    public object PropertyValue { get; set; }
}