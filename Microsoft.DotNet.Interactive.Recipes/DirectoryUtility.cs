using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Interactive.Recipes
{
    internal static class DirectoryUtility
    {
        private static readonly object CreateDirectoryLock = new object();

        private static readonly DirectoryInfo _defaultDirectory = new DirectoryInfo(
            Path.Combine(
                Paths.UserProfile,
                ".net-interactive-tests"));

        public static DirectoryInfo CreateDirectory(
            [CallerMemberName] string folderNameStartsWith = null,
            DirectoryInfo parentDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(folderNameStartsWith))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(folderNameStartsWith));
            }

            parentDirectory = parentDirectory ?? _defaultDirectory;

            DirectoryInfo created;

            lock (CreateDirectoryLock)
            {
                if (!parentDirectory.Exists)
                {
                    parentDirectory.Create();
                }

                var existingFolders = parentDirectory.GetDirectories($"{folderNameStartsWith}.*");

                created = parentDirectory.CreateSubdirectory($"{folderNameStartsWith}.{existingFolders.Length + 1}");
            }

            return created;
        }
    }
}