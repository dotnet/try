// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import { ApiMessage, isApiMessageOfType, isApiMessageCorrelatedTo, SERVICE_ERROR_RESPONSE } from "./apiMessages";
import { ServiceError } from "../session";


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

export function responseOrErrorFor<T, E>(messageBus: IMessageBus, responseMessageType: string, erroreMessageType: string, requestId: string, responseGenerator: (responseMessage: ApiMessage) => T, errorGenerator: (errorMessage: ApiMessage) => E): Promise<T> {
    let ret = new Promise<T>((resolve, reject) => {
        let sub = messageBus.subscribe({
            next: (message) => {
                if (isApiMessageOfType(message, responseMessageType) && isApiMessageCorrelatedTo(message, requestId)) {
                    let result = responseGenerator(message);
                    sub.unsubscribe();
                    resolve(<T>result);
                }

                else if (isApiMessageOfType(message, erroreMessageType) && isApiMessageCorrelatedTo(message, requestId)) {
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