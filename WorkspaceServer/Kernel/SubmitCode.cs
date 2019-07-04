// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace WorkspaceServer.Kernel
{
    public class SubmitCode : KernelCommandBase
    {
        public string Value { get; }

        public SubmitCode(string value) 
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public SubmitCode(string value, Guid id, Guid parentId) : base(id, parentId)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}