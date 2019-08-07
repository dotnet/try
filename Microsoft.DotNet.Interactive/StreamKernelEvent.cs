namespace Microsoft.DotNet.Interactive
{
    public class StreamKernelEvent
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Event { get; set; }
    }
}
