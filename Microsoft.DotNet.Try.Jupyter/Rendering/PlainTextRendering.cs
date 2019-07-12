// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using WorkspaceServer.Kernel;

namespace Microsoft.DotNet.Try.Jupyter.Rendering
{
    public class PlainTextRendering : IRendering
    {
        public PlainTextRendering(string text)
        {
            Content = text;
        }

        public string MimeType { get; } = "text/plain";
        public object Content { get; }
    }
}