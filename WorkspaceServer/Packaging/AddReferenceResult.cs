
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace WorkspaceServer.Packaging
{
    public partial class PackageRestoreContext
    {
        public class AddReferenceResult
        {
            public AddReferenceResult(bool succeeded, MetadataReference[] references = null, string installedVersion = null, IEnumerable<string> detailedErrors = null)
            {
                Succeeded = succeeded;
                References = references ?? Array.Empty<MetadataReference>();
                InstalledVersion = installedVersion;
                DetailedErrors = detailedErrors;
            }

            public bool Succeeded { get; }
            public MetadataReference[] References { get; }
            public string InstalledVersion { get; }
            public IEnumerable<string> DetailedErrors { get; }
        }
    }
}
