// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using WorkspaceServer.Packaging;

namespace WorkspaceServer
{
    public class FindPackageInDefaultLocation : IPackageFinder
    {
        private readonly IDirectoryAccessor _directoryAccessor;

        public FindPackageInDefaultLocation(IDirectoryAccessor directoryAccessor = null)
        {
            _directoryAccessor = directoryAccessor ??
                                 new FileSystemDirectoryAccessor(Package.DefaultPackagesDirectory);
        }

        public Task<T> Find<T>(PackageDescriptor descriptor)
            where T : IPackage
        {
            if (!descriptor.IsPathSpecified)
            {

                if (_directoryAccessor.DirectoryExists(descriptor.Name))
                {
                    var directoryAccessor = _directoryAccessor.GetDirectoryAccessorForRelativePath(descriptor.Name);

                    var pkg = new Package2(
                        descriptor,
                        directoryAccessor);

                    if (pkg is T t)
                    {
                        return Task.FromResult(t);
                    }
                }
            }

            return Task.FromResult<T>(default);
        }
    }
}