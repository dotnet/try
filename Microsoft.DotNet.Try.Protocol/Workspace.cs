// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.DotNet.Try.Protocol
{
    public class Workspace
    {
        private const string DefaultWorkspaceType = "script";

        public Workspace(
            string[] usings = null,
            File[] files = null,
            Buffer[] buffers = null,
            string workspaceType = DefaultWorkspaceType,
            bool includeInstrumentation = false)
        {
            WorkspaceType = workspaceType ?? DefaultWorkspaceType;
            Usings = usings ?? Array.Empty<string>();
            Usings = usings ?? Array.Empty<string>();
            Files = files ?? Array.Empty<File>();
            Buffers = buffers ?? Array.Empty<Buffer>();

            IncludeInstrumentation = includeInstrumentation;

            if (Files.Distinct().Count() != Files.Length )
            {
                throw new ArgumentException($"Duplicate file names:{Environment.NewLine}{string.Join(Environment.NewLine, Files.Select(f => f.Name))}");
            }
            
            if (Buffers.Distinct().Count() != Buffers.Length )
            {
                throw new ArgumentException($"Duplicate buffer ids:{Environment.NewLine}{string.Join(Environment.NewLine, Buffers.Select(b => b.Id))}");
            }
        }

        public File[] Files { get; }

        public string[] Usings { get; }

        public string WorkspaceType { get; }

        public bool IncludeInstrumentation { get; }

        [Required]
        [MinLength(1)]
        public Buffer[] Buffers { get; }

        public static Workspace FromSource(
            string source,
            string workspaceType,
            string id = "Program.cs",
            string[] usings = null,
            int position = 0)
        {
            return new Workspace(
                workspaceType: workspaceType,
                buffers: new[]
                {
                    new Buffer(BufferId.Parse(id ?? throw new ArgumentNullException(nameof(id))), source, position)
                },
                usings: usings);
        }

        public static Workspace FromSources(
            string workspaceType = null,
            params (string id, string content, int position)[] sources) =>
            new Workspace(
                workspaceType: workspaceType,
                buffers: sources.Select(s => new Buffer(BufferId.Parse(s.id), s.content, s.position)).ToArray());
    }
}
