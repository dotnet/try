// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.IsCompleteReply)]
    public class IsCompleteReply : JupyterMessageContent
    {
        //One of 'complete', 'incomplete', 'invalid', 'unknown'
        [JsonProperty("status")]
        public string Status { get; set; }

        //If status is 'incomplete', indent should contain the characters to use
        //to indent the next line. This is only a hint: frontends may ignore it
        // and use their own autoindentation rules. For other statuses, this
        // field does not exist.
        [JsonProperty("ident")]
        public string Ident { get; set; }
    }
}