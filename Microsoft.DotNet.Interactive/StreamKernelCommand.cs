namespace Microsoft.DotNet.Interactive
{
    public class StreamKernelCommand
    {
        public int Id { get; set; }
        public string CommandType { get; set; }
        public object Command { get; set; }
    }
}
