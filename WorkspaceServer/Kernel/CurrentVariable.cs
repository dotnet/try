// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis.Scripting;

namespace WorkspaceServer.Kernel
{
    internal class CurrentVariable
    {
        internal CurrentVariable(ScriptVariable variable)
        {
            Name = variable.Name;
            Type = variable.Type;
            Value = variable.Value;
        }

        public object Value { get; }

        public Type Type { get; }

        public string Name { get; }
    }
}