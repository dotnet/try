// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MLS.Agent
{
    public class BrowserLaunchUri
    {
        public BrowserLaunchUri(string scheme, string host, ushort port)
        {
            Scheme = scheme ?? throw new ArgumentNullException(nameof(scheme));
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
        }

        public string Scheme { get; }
        public string Host { get; }
        public ushort Port { get; }

        public override string ToString()
        {
            return $"{Scheme}://{Host}:{Port}";
        }
    }
}