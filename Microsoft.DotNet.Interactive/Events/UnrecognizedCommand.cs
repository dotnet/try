﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events
{
    internal class UnrecognizedCommand : IKernelEvent
    {
        public IKernelCommand Command => null;
        public string Body { get; set; }
    }
}
