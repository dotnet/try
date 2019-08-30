// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.FSharp;

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
{ReferenceFromType(typeof(FSharpPocketViewTags))}
open {typeof(FSharpPocketViewTags).FullName}
"))).Wait();

            return kernel;
        }

        private static string ReferenceFromType(Type type)
        {
            return $@"#r ""{type.Assembly.Location.Replace("\\", "\\\\")}""";
        }
    }
}
