// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import * as apiMessages from "../apiMessages";
import { ServiceError } from "../session";
import * as dotnetInteractive from "@microsoft/dotnet-interactive";
import * as newContract from "../newContract";


export function responseFor<T>(messageBus: IMessageBus, responseMessageType: string, requestId: string, responseGenerator: (responseMessage: { type: string, requestId?: string, [key: string]: any }) => T): Promise<T> {

    return responseOrErrorFor<T, ServiceError>(
        messageBus,
        responseMessageType,
        apiMessages.SERVICE_ERROR_RESPONSE,
        requestId,
        responseGenerator, (errorMessage) => {
            let result: any = {
                ...errorMessage,
            };
            delete result.type;
            return result;
        });
}

export function responseOrErrorFor<T, E>(messageBus: IMessageBus, responseMessageType: string, erroreMessageType: string, requestId: string, responseGenerator: (responseMessage: { type: string, requestId?: string, [key: string]: any }) => T, errorGenerator: (errorMessage: { type: string, requestId?: string, [key: string]: any }) => E): Promise<T> {

    dotnetInteractive.Logger.default.info(`---- setting up response awaiter for [${requestId}] and type [${responseMessageType}]`);
    let ret = new Promise<T>((resolve, reject) => {
        let sub = messageBus.subscribe({
            next: (message) => {
                const m: { type: string, requestId?: string } = message;
                if (newContract.isMessageOfType(message, responseMessageType) && newContract.isMessageCorrelatedTo(m, requestId)) {
                    dotnetInteractive.Logger.default.info(`---- resolving response awaiter for [${requestId}] and type [${responseMessageType}]`);
                    let result = responseGenerator(message);
                    sub.unsubscribe();
                    resolve(<T>result);
                }

                else if (newContract.isMessageOfType(message, erroreMessageType) && newContract.isMessageCorrelatedTo(m, requestId)) {
                    dotnetInteractive.Logger.default.info(`---- rejecting response awaiter for [${requestId}] and type [${responseMessageType}] : reason [${JSON.stringify(message)}]`);
                    let result = errorGenerator(message);
                    sub.unsubscribe();
                    reject(<E>result);
                }
            },
            error:

                error => {
                    dotnetInteractive.Logger.default.info(`---- rejecting response awaiter for [${requestId}] and type [${responseMessageType}] : reason [${JSON.stringify(error)}]`);
                    sub.unsubscribe();
                    reject(error);
                }
        });
    });

    return ret;
}