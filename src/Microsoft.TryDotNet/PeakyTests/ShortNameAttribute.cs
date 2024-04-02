// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TryDotNet.PeakyTests;

internal class ShortNameAttribute : Attribute
{
    public string Name;

    public ShortNameAttribute(string v)
    {
        this.Name = v;
    }
}