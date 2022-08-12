// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';
export interface IWasmRunner {
    (runRequest: {
        assembly: dotnetInteractive.Base64EncodedAssembly,
        onOutput: (output: string) => void,
        onError: (error: string) => void,
    }): Promise<void>
}
