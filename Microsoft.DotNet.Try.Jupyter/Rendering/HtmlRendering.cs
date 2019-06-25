// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    public class HtmlRendering : IRendering
    {
        public HtmlRendering(string html)
        {
            Content = html;
        }

        public string Mime { get; } = "text/html";
        public object Content { get; }
    }
}