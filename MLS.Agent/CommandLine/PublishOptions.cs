using MLS.Agent.Tools;

namespace MLS.Agent.CommandLine
{
    public class PublishOptions
    {
        public PublishOptions(IDirectoryAccessor rootDirectory)
        {
            RootDirectory = rootDirectory ?? throw new System.ArgumentNullException(nameof(rootDirectory));
        }

        public IDirectoryAccessor RootDirectory { get; }
    }
}