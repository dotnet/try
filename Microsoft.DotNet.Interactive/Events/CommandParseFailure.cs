using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    internal class CommandParseFailure : IKernelEvent
    {
        public IKernelCommand Command => null;
        public string Body { get; set; }
    }
}
