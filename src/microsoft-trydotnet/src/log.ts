// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';

export function configureLogging(configuration: { enableLogging: boolean }) {
    if (configuration.enableLogging === true) {
        dotnetInteractive.Logger.configure("trydotnet-js", (entry) => {
            switch (entry.logLevel) {
                case dotnetInteractive.LogLevel.Info:
                    console.log(`[${entry.source}] ${entry.message}`);
                    break;
                case dotnetInteractive.LogLevel.Warn:
                    console.warn(`[${entry.source}] ${entry.message}`);
                    break;
                case dotnetInteractive.LogLevel.Error:
                    console.error(`[${entry.source}] ${entry.message}`);
                    break;
            }

        });
    } else {
        dotnetInteractive.Logger.configure("trydotnet-js", (_entry) => { });
    }
}