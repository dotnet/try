// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace WorkspaceServer.Kernel
{
    public class ValueProduced: KernelEventBase
    {
        public ValueProduced(object value, SubmitCode submitCode) : base(submitCode)
        {
            Value = value;
        }

        public object Value { get; }
    }
}