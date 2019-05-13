// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.ML.Data;

namespace IrisClustering.DataStructures
{
    // IrisPrediction is the result returned from prediction operations
    public class IrisPrediction
    {
        [ColumnName("PredictedLabel")]
        public uint SelectedClusterId;

        [ColumnName("Score")]
        public float[] Distance;
    }
}
