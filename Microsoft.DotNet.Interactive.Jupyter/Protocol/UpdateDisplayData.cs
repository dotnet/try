// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    [JupyterMessageType(JupyterMessageContentTypes.UpdateDisplayData)]
    public class UpdateDisplayData : DisplayData
    {
        public UpdateDisplayData(string source = null, IReadOnlyDictionary<string, object> data = null, IReadOnlyDictionary<string, object> metaData = null, IReadOnlyDictionary<string, object> transient = null) : base(source, data, metaData, transient)
        {
        }
    }
}