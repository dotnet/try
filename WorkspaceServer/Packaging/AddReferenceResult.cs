
using System;
using Microsoft.CodeAnalysis;

namespace WorkspaceServer.Packaging
{
    public partial class PackageRestoreContext
    {
        public class AddReferenceResult
        {
            public AddReferenceResult(bool succeeded, MetadataReference[] references = null)
            {
                Succeeded = succeeded;
                References = references ?? Array.Empty<MetadataReference>();
            }

            public bool Succeeded { get; }
            public MetadataReference[] References { get; }
        }
    }
}
