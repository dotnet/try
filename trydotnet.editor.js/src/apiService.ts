// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';

export interface IApiServiceConfiguration {
    referrer: URL;
}

export function createApiService(): IApiService {
    const configuration = {
        referrer: new URL("set referrer")
    };

    return createApiServiceWithConfiguration(configuration);
}
export interface IApiService {
    (commands: dotnetInteractive.KernelCommandEnvelope[]): Promise<dotnetInteractive.KernelEventEnvelope[]>
}


function createApiServiceWithConfiguration(configuration: IApiServiceConfiguration): IApiService {
    let service: IApiService = async (commands) => {

        throw new Error("Method not implemented.");
    };

    return service;
}