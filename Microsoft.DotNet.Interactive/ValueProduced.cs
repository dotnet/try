// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace WorkspaceServer.Kernel
{
    public class ValueProduced : KernelEventBase
    {
        public ValueProduced(
            object value,
            SubmitCode submitCode,
            IReadOnlyCollection<FormattedValue> formattedValues = null) : base(submitCode)
        {
            Value = value;
            FormattedValues = formattedValues;
        }

        public object Value { get; }

        public IReadOnlyCollection<FormattedValue> FormattedValues { get; }
    }
}