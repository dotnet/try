// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import { ApiMessage, isApiMessageOfType, isApiMessageCorrelatedTo, SERVICE_ERROR_RESPONSE } from "../apiMessages";
import { ServiceError } from "../session";
import { Logger } from "@microsoft/dotnet-interactive";


export function responseFor<T>(messageBus: IMessageBus, responseMessageType: string, requestId: string, responseGenerator: (responseMessage: ApiMessage) => T): Promise<T> {

    return responseOrErrorFor<T, ServiceError>(
        messageBus,
        responseMessageType,
        SERVICE_ERROR_RESPONSE,
        requestId,
        responseGenerator, (errorMessage) => {
            let result: any = {
                ...errorMessage,
            };
            delete result.type;
            return result;
        });
}

export function responseOrErrorFor<T, E>(messageBus: IMessageBus, responseMessageType: string, erroreMessageType: string, requestId: string, responseGenerator: (responseMessage: { type: string, requestId?: string }) => T, errorGenerator: (errorMessage: { type: string, requestId?: string }) => E): Promise<T> {

    Logger.default.info(`---- setting up response awaiter for [${requestId}] and type [${responseMessageType}]`);
    let ret = new Promise<T>((resolve, reject) => {
        let sub = messageBus.subscribe({
            next: (message) => {
                const m: { type: string, requestId?: string } = message;
                if (isApiMessageOfType(message, responseMessageType) && isApiMessageCorrelatedTo(m, requestId)) {
                    Logger.default.info(`---- resolving response awaiter for [${requestId}] and type [${responseMessageType}]`);
                    let result = responseGenerator(message);
                    sub.unsubscribe();
                    resolve(<T>result);
                }

                else if (isApiMessageOfType(message, erroreMessageType) && isApiMessageCorrelatedTo(m, requestId)) {
                    Logger.default.info(`---- rejecting response awaiter for [${requestId}] and type [${responseMessageType}]`);
                    let result = errorGenerator(message);
                    sub.unsubscribe();
                    reject(<E>result);
                }
            },
            error:

                error => {
                    sub.unsubscribe();
                    reject(error);
                }
        });
    });

    return ret;
}