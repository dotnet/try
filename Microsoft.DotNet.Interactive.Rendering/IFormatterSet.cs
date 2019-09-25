﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Rendering
{
    internal interface IFormatterSet
    {
        void AddFormatterFactoryForOpenGenericType(
            Type type,
            Func<Type, ITypeFormatter> getFormatter);

        bool TryGetFormatterForType(Type type, out ITypeFormatter formatter);
    }
}