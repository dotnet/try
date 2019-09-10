// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public interface IKernelStatus
    {
        void SetAsBusy();

        void SetAsIdle();

        Task Idle();
    }
}