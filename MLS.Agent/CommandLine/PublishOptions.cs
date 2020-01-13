using MLS.Agent.Tools;

namespace MLS.Agent.CommandLine
{
    public class PublishOptions
    {
        public PublishOptions(IDirectoryAccessor rootDirectory, IDirectoryAccessor targetDirectory, PublishFormat format)
        {
            RootDirectory = rootDirectory ?? throw new System.ArgumentNullException(nameof(rootDirectory));
            Format = format;
            TargetDirectory = targetDirectory;
        }

        public IDirectoryAccessor RootDirectory { get; }

        public IDirectoryAccessor TargetDirectory { get; }

        public PublishFormat Format { get; }
    }

    public enum PublishFormat
    {
        Markdown,
        HTML
    }
}