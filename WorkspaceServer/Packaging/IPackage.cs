// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;

namespace WorkspaceServer.Packaging
{
    public interface IPackage
    {
        string Name { get; }
    }

    public interface IHaveADirectory : IPackage
    {
        DirectoryInfo Directory { get; }
    }

    public interface IHaveADirectoryAccessor : IPackage
    {
        IDirectoryAccessor Directory { get; }
    }

    public interface IMightSupportBlazor : IPackage
    {
        bool CanSupportBlazor { get; }
    }

    public interface ICreateWorkspaceForRun : IPackage
    {
        Task<Workspace> CreateRoslynWorkspaceForRunAsync(Budget budget);
    }

    public interface ICreateWorkspaceForLanguageServices : IPackage
    {
        Task<Workspace> CreateRoslynWorkspaceForLanguageServicesAsync(Budget budget);
    }
}