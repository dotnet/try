// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';

export interface IServiceError {
    statusCode: string;
    message: string;
    requestId?: string;
};

export interface IApiServiceConfiguration {
    referer?: URL;
    commandsUrl: URL;
    onServiceError: (error: IServiceError) => void;
}


export function createApiService(configuration: IApiServiceConfiguration): IApiService {
    return createApiServiceWithConfiguration(configuration);
}
export interface IApiService {
    (commands: polyglotNotebooks.KernelCommandEnvelope[]): Promise<polyglotNotebooks.KernelEventEnvelope[]>
}


function createApiServiceWithConfiguration(configuration: IApiServiceConfiguration): IApiService {
    let service: IApiService = async (commands) => {
        let bodyContent = JSON.stringify({
            commands: commands.map(command => command.toJson())
        });
        let headers = {
            'Content-Type': 'application/json'
        };
        if (configuration.referer) {
            headers['Referer'] = configuration.referer.toString();
        }

        let response = await fetch(configuration.commandsUrl.toString(), {
            method: 'POST',
            headers: headers,
            body: bodyContent
        });

        polyglotNotebooks.Logger.default.info(`[ApiService.request] ${bodyContent}`);

        if (!response.ok) {
            configuration.onServiceError({
                statusCode: `${response.status}`,
                message: response.statusText
            });
            throw new Error(`${response.status} ${response.statusText}`);
        }

        let json = await response.json();

        polyglotNotebooks.Logger.default.info(`[ApiService.response] ${JSON.stringify(json)}`);

        const srcEvents = json.events as polyglotNotebooks.KernelEventEnvelopeModel[];
        return srcEvents.map(srcEvent => polyglotNotebooks.KernelEventEnvelope.fromJson(srcEvent));
    };

    return service;
}