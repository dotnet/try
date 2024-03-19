// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#nullable disable

using System.Reflection;

namespace Microsoft.TryDotNet.PeakyTests;

internal static class TypeExtensions
{
    static readonly Type[] EmptyTypeArray = new Type[] { };

    public static bool HasDefaultConstructor(this Type type)
    {
        type = type ?? throw new ArgumentNullException(nameof(type));

        return type.GetDefaultConstructor() != null;
    }

    public static ConstructorInfo GetDefaultConstructor(this Type type)
    {
        return type.GetConstructor(EmptyTypeArray);
    }
}