// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    public class MessageTypeValues
    {
        public const string ExecuteRequest = "execute_request";

        public const string ExecuteReply = "execute_reply";

        public const string ExecuteResult = "execute_result";

        public const string ExecuteInput = "execute_input";

        public const string KernelInfoRequest = "kernel_info_request";

        public const string KernelInfoReply = "kernel_info_reply";

        public const string KernelShutdownRequest = "shutdown_request";

        public const string KernelShutdownReply = "shutdown_reply";

        public const string CompleteRequest = "complete_request";

        public const string IsCompleteRequest = "is_complete_request";

        public const string IsCompleteReply = "is_complete_reply";

        public const string Status = "status";

        public const string Stream = "stream";

        public const string Error = "error";

        public const string DisplayData = "display_data";

        public const string UpdateDisplayData = "update_display_data";

    }
}
