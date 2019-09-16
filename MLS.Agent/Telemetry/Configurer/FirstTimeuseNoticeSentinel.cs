// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MLS.Agent.Telemetry.Utils;

namespace MLS.Agent.Telemetry.Configurer
{
    // TODO: This isn't fully realized yet. Do we want to have this anyway?
    internal class FirstTimeUseNoticeSentinel : IFirstTimeUseNoticeSentinel
    {
        public static readonly string SENTINEL = $"{Product.Version}.tryFirstUseSentinel";

        public void CreateIfNotExists()
        {
        }

        public void Dispose()
        {
        }

        public bool Exists()
        {
            return true;
        }
    }
}
