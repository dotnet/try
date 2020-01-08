using MLS.Agent.Tools;

namespace MLS.Agent.CommandLine
{
    public class SyncOptions
    {
        public SyncOptions(IDirectoryAccessor rootDirectory)
        {
            RootDirectory = rootDirectory ?? throw new System.ArgumentNullException(nameof(rootDirectory));
        }

        public IDirectoryAccessor RootDirectory { get; }
    }
}