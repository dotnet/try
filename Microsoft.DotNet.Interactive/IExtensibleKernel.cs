// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Tools;

namespace Microsoft.DotNet.Interactive
{
    public interface IExtensibleKernel
    {
        RelativeDirectoryPath ExtensionsPath { get; }
    }
}