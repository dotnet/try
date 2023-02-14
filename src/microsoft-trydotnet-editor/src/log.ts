// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';

export function configureLogging(configuration: { enableLogging: boolean }) {
    if (configuration.enableLogging === true) {
        polyglotNotebooks.Logger.configure("trydotnet-editor", (entry) => {
            switch (entry.logLevel) {
                case polyglotNotebooks.LogLevel.Info:
                    console.log(`[${entry.source}] ${entry.message}`);
                    break;
                case polyglotNotebooks.LogLevel.Warn:
                    console.warn(`[${entry.source}] ${entry.message}`);
                    break;
                case polyglotNotebooks.LogLevel.Error:
                    console.error(`[${entry.source}] ${entry.message}`);
                    break;
            }

        });
    } else {
        polyglotNotebooks.Logger.configure("trydotnet-editor", (_entry) => { });
    }
}