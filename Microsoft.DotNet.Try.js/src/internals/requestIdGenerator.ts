// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import { ApiMessage, CREATE_OPERATION_ID_REQUEST, CREATE_OPERATION_ID_RESPONSE } from "./apiMessages";
import { from } from "rxjs";
import { timeoutWith } from "rxjs/operators"
import { responseFor } from "./responseFor";

export interface IRequestIdGenerator {
    getNewRequestId(): Promise<string>;
}

export class RequestIdGenerator implements IRequestIdGenerator {

    private id: string;
    private seed: number;
    constructor(private messageBus: IMessageBus, private requestTimeoutMs: number = 2000) {
        if (!this.messageBus) {
            throw new Error("messageBus cannot be null or undefined")
        }

        let random = new Date().getTime();
        this.id = `trydotnetjs.session.${random}`;
        this.seed = 0;

    }
    public getNewRequestId(): Promise<string> {
        const requestId = `${this.id}_${this.seed++}`;

        let request: ApiMessage = {
            type: CREATE_OPERATION_ID_REQUEST,
            requestId: requestId
        }

        let ret = responseFor<string>(this.messageBus, CREATE_OPERATION_ID_RESPONSE, requestId, (responseMessage) => {
            let result: string = (<{ operationId: string }>responseMessage).operationId;
            return <string>result
        });

        let composed = from(ret).pipe(timeoutWith(this.requestTimeoutMs, from([requestId]))).toPromise();

        this.messageBus.post(request);

        return composed;
    }
}