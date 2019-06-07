// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Try.Jupyter.Protocol
{
    [JupyterMessageType(MessageTypeValues.IntrospectionReply)]
    public class IntrospectionReply
    {
        public static IntrospectionReply Ok(string source, object data, object metaData) => new IntrospectionReply{Status = StatusValues.Ok, Source = source, Data = data, MetaData = metaData};
        public static IntrospectionReply Error(string source, object data, object metaData) => new IntrospectionReply { Status = StatusValues.Error, Source = source, Data = data, MetaData = metaData };
        protected IntrospectionReply()
        {
            Source = string.Empty;
            Data = new object();
            MetaData = new object();
        }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public object MetaData { get; set; }
    }
}