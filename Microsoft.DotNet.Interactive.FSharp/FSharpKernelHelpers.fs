﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open Microsoft.DotNet.Interactive

module FSharpKernelHelpers =
    let display (value: obj) =
        Kernel.display value
    let Javascript (content: string) =
        Kernel.Javascript content
