// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using WorkspaceServer.Models.Instrumentation;

namespace WorkspaceServer.Servers.Roslyn.Instrumentation
{
    public class ProgramOutputStreams
    {
        public ProgramOutputStreams(IReadOnlyCollection<string> stdOut, IReadOnlyCollection<string> instrumentation, string programDescriptor = "")
        {
            StdOut = stdOut ?? Array.Empty<string>();
            ProgramStatesArray = new ProgramStateAtPositionArray(instrumentation);
            ProgramDescriptor = JsonConvert.DeserializeObject<ProgramDescriptor>(programDescriptor);
        }

        public IReadOnlyCollection<string> StdOut { get; }

        public ProgramStateAtPositionArray ProgramStatesArray { get; }

        public ProgramDescriptor ProgramDescriptor { get; }
    }
}
