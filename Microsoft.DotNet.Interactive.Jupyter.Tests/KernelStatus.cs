// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Try.Jupyter;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    class KernelStatus : IRequestHandlerStatus
    {
        public void SetAsBusy()
        {
            IsBusy = true;
        }

        public void SetAsIdle()
        {
            IsBusy = false;
        }

        public bool IsBusy { get; private set; }
    }
}