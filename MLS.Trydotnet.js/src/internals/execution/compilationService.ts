// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Workspace } from "../workspace";
import { CompilationResult, ServiceError } from "../../session";
import { COMPILE_RESPONSE, ApiMessage, COMPILE_REQUEST } from "../apiMessages";
import { responseFor } from "../responseFor";
import { IMessageBus } from "../messageBus";
import { RequestIdGenerator } from "../requestIdGenerator";
import { Observer } from "rxjs";

enum compilationServiceStates {
    ready,
    running
}
export class compilationService {
    private state: compilationServiceStates = compilationServiceStates.ready;
    private currentCompile: { requestId: string, request: Promise<CompilationResult> };

    constructor(private messageBus: IMessageBus, private requestIdGenerator: RequestIdGenerator, private serviceErrorChannel : Observer<ServiceError>) {
        if (!this.messageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }
    }

    async compile(workspace: Workspace): Promise<CompilationResult> {
        if (!workspace) {
            throw new Error("workspace cannot be null");
        }

        if (this.state === compilationServiceStates.running) {
            return this.currentCompile.request;
        }


        const requestId = await this.requestIdGenerator.getNewRequestId();

        let request: ApiMessage = {
            type: COMPILE_REQUEST,
            requestId: requestId
        }

        let ret = responseFor<CompilationResult>(this.messageBus, COMPILE_RESPONSE, requestId, (responseMessage) => {
            let result: any = {
                ...responseMessage,
                succeeded: (<any>responseMessage).outcome === "Success"
            };
            delete result.type;
            delete result.outcome;
            return <CompilationResult>result
        });

        this.state = compilationServiceStates.running;

        this.currentCompile = {
            requestId,
            request: ret
        };

        this.messageBus.post(request);

        return ret;
    }
}