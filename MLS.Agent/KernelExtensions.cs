// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using XPlot.DotNet.Interactive.KernelExtensions;

namespace MLS.Agent
{
    public static class KernelExtensions
    {
        public static T UseXplot<T>(this T kernel)
            where T : KernelBase
        {
            var plotlyKernelExtension = new PlotlyKernelExtension();
            Task.Run(() => plotlyKernelExtension.OnLoadAsync(kernel)).Wait();

            var googleKernelExtension = new GoogleChartKernelExtension();
            Task.Run(() => googleKernelExtension.OnLoadAsync(kernel)).Wait();
            return kernel;
        }
    }
}
