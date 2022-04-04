// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';

export interface IApiServiceConfiguration {
    referer?: URL;
    commandsUrl: URL;
}

export function createApiService(configuration: IApiServiceConfiguration): IApiService {


    return createApiServiceWithConfiguration(configuration);
}
export interface IApiService {
    (commands: dotnetInteractive.KernelCommandEnvelope[]): Promise<dotnetInteractive.KernelEventEnvelope[]>
}


function createApiServiceWithConfiguration(configuration: IApiServiceConfiguration): IApiService {
    let service: IApiService = async (commands) => {

        let headers = {
            'Content-Type': 'application/json'
        };
        if (configuration.referer) {
            headers['Referer'] = configuration.referer.toString();
        }

        let response = await fetch(configuration.commandsUrl.toString(), {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(commands)
        });

        return response.json();
    };

    return service;
}