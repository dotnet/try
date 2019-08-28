using MLS.Agent.Tools;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class NuGetPackageKernelExtensionFinder
    {
        public static IEnumerable<FileInfo> FindExtensionDlls(IDirectoryAccessor nugetPackageAssemblyDirectory, string packageName)
        {
            var directory = nugetPackageAssemblyDirectory.GetFullyQualifiedRoot();
            while (directory != null && directory.Parent != null && directory.Parent.Name.ToLower().CompareTo(packageName.ToLower()) != 0)
            {
                directory = directory.Parent;
            }

            var directoryContainingExtensions = nugetPackageAssemblyDirectory.GetDirectoryAccessorFor(directory);
            var extensionsDirectory = directoryContainingExtensions.GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath("interactive-extensions"));
            return extensionsDirectory.GetAllFiles().Where(file => file.Extension == ".dll").Select(file => extensionsDirectory.GetFullyQualifiedFilePath(file));
        }
    }
}
