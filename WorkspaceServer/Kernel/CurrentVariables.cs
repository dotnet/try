// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting;

namespace WorkspaceServer.Kernel
{
    public class CurrentVariables : IEnumerable<CurrentVariable>
    {
        private readonly Dictionary<string, CurrentVariable> _variables = new Dictionary<string, CurrentVariable>();

        public CurrentVariables(IEnumerable<ScriptVariable> scriptVariables)
        {
            foreach (var variable in scriptVariables)
            {
                _variables[variable.Name] = new CurrentVariable(variable);
            }
        }

        public IEnumerator<CurrentVariable> GetEnumerator()
        {
            return _variables.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}