// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Try.Markdown
{
    public sealed class SuccessfulCodeFenceOptionParseResult : CodeFenceOptionsParseResult
    {
        public SuccessfulCodeFenceOptionParseResult(CodeBlockAnnotations annotations)
        {
            Annotations = annotations;
        }

        public CodeBlockAnnotations Annotations { get; }
    }
}