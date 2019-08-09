﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Clockwise;
using Microsoft.DotNet.Interactive;
using XPlot.DotNet.Interactive.KernelExtensions;

namespace MLS.Agent
{
    public static class KernelExtensions
    {
        public static T UseDefaultExtensions<T>(this T kernel)
            where T : KernelBase
        {

            var extension = new XPlotKernelExtension();
            extension.OnLoadAsync(kernel).Wait();
            return kernel;
        }
    }
}
