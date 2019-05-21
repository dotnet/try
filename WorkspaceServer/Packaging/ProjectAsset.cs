// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;

namespace WorkspaceServer.Packaging
{
    public class ProjectAsset : PackageAsset,
        ICreateWorkspaceForLanguageServices,
        ICreateWorkspaceForRun

    {
        public ProjectAsset(IDirectoryAccessor directoryAccessor) : base(directoryAccessor)
        {
            Name = directoryAccessor.GetFullyQualifiedRoot().Name;
        }

        public string Name { get; }
        public Task<Workspace> CreateRoslynWorkspaceAsync(Budget budget)
        {
            throw new System.NotImplementedException();
        }

        public Task<Workspace> CreateRoslynWorkspaceForRunAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }

        public Task<Workspace> CreateRoslynWorkspaceForLanguageServicesAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }

  
    }
}