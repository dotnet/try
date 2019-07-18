// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace WorkspaceServer.Servers.Scripting
{
    internal class WorkspaceFixture
    {
        private readonly AdhocWorkspace _workspace = new AdhocWorkspace(MefHostServices.DefaultHost);
        private readonly DocumentId _documentId;

        public WorkspaceFixture(
            CompilationOptions compilationOptions,
            ImmutableArray<MetadataReference> metadataReferences)
        {
            if (compilationOptions == null)
            {
                throw new ArgumentNullException(nameof(compilationOptions));
            }
            if (metadataReferences == null)
            {
                throw new ArgumentNullException(nameof(metadataReferences));
            }
            var projectId = ProjectId.CreateNewId("ScriptProject");

            var projectInfo = ProjectInfo.Create(
                projectId,
                version: VersionStamp.Create(),
                name: "ScriptProject",
                assemblyName: "ScriptProject",
                language: LanguageNames.CSharp,
                compilationOptions: compilationOptions,
                metadataReferences: metadataReferences);

            _workspace.AddProject(projectInfo);

            _documentId = DocumentId.CreateNewId(projectId, "ScriptDocument");

            var documentInfo = DocumentInfo.Create(_documentId,
                name: "ScriptDocument",
                sourceCodeKind: SourceCodeKind.Script);

            _workspace.AddDocument(documentInfo);
        }


        public WorkspaceFixture(
            IEnumerable<string> defaultUsings,
            ImmutableArray<MetadataReference> metadataReferences) : this(new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            usings: defaultUsings), metadataReferences)
        {
            
        }

        public Document ForkDocument(string text)
        {
            var document = _workspace.CurrentSolution.GetDocument(_documentId);
            return document.WithText(SourceText.From(text));
        }
    }
}