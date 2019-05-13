// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MLS.Agent.Tests
{
    public class FakeBrowserLauncher : IBrowserLauncher
    {
        public void LaunchBrowser(Uri uri)
        {
            LaunchedUri = uri;
        }

        public Uri LaunchedUri { get; private set; }
    }
}