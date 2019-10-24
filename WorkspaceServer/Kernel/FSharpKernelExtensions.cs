// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Rendering;
using XPlot.Plotly;

namespace WorkspaceServer.Kernel
{
    public static class FSharpKernelExtensions
    {
        public static FSharpKernel UseDefaultRendering(
            this FSharpKernel kernel)
        {
            Task.Run(() =>
                         kernel.SendAsync(
                         new SubmitCode($@"
{ReferenceFromType(typeof(IHtmlContent))}
{ReferenceFromType(typeof(IKernel))}
{ReferenceFromType(typeof(FSharpPocketViewTags))}
{ReferenceFromType(typeof(PlotlyChart))}
{ReferenceFromType(typeof(Formatter))}
open {typeof(IHtmlContent).Namespace}
open {typeof(FSharpPocketViewTags).FullName}
open {typeof(FSharpPocketViewTags).Namespace};
open {typeof(PlotlyChart).Namespace}
open {typeof(Formatter).Namespace}
"))).Wait();
            return kernel;
        }

        public static FSharpKernel UseDefaultNamespaces(
            this FSharpKernel kernel)
        {
            Task.Run(() =>
                         kernel.SendAsync(
                         new SubmitCode($@"
open System
open System.Text
open System.Threading.Tasks
open System.Linq
"))).Wait();
            return kernel;
        }

        public static FSharpKernel UseKernelHelpers(
            this FSharpKernel kernel)
        {
            Task.Run(() =>
                kernel.SendAsync(new SubmitCode($@"
open {typeof(FSharpKernelHelpers).FullName}
"))).Wait();

            return kernel;
        }

        private static string ReferenceFromType(Type type)
        {
            return $@"#r ""{type.Assembly.Location.Replace("\\", "/")}""";
        }
    }
}
