// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RunConfiguration, RunResult, OutputEvent, ServiceError } from "../../session";
import { Workspace } from "../workspace";
import { ApiMessage, RUN_REQUEST, RUN_RESPONSE, SERVICE_ERROR_RESPONSE, isApiMessageOfType } from "../apiMessages";
import { responseFor } from "../responseFor";
import { IMessageBus } from "../messageBus";
import { RequestIdGenerator } from "../requestIdGenerator";
import { Subject, Subscribable, PartialObserver, Unsubscribable, Observer } from "rxjs";

enum executionServiceStates {
    ready,
    running
}
export class executionService implements Subscribable<OutputEvent>{
    private state: executionServiceStates = executionServiceStates.ready;
    private outputEventChannel: Subject<OutputEvent> = new Subject<OutputEvent>();
    private currentRun: { requestId: string, request: Promise<RunResult> };

    constructor(private messageBus: IMessageBus, private requestIdGenerator: RequestIdGenerator, private serviceErrorChannel : Observer<ServiceError>) {
        if (!this.messageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }
    }

    public async run(workspace: Workspace, configuration?: RunConfiguration): Promise<RunResult> {
        if (!workspace) {
            throw new Error("workspace cannot be null");
        }

        if (this.state === executionServiceStates.running) {
            return this.currentRun.request;
        }

        const requestId = await this.requestIdGenerator.getNewRequestId();

        let request: ApiMessage = {
            type: RUN_REQUEST,
            requestId: requestId
        }

        if (configuration) {
            request.parameters = JSON.parse(JSON.stringify(configuration));
        }

        let resultChannel = this.outputEventChannel;
        let serviceErrorChannel = this.serviceErrorChannel;

        let ret = responseFor<RunResult>(this.messageBus, RUN_RESPONSE, requestId, (responseMessage) => {
            let result: any = {
                ...responseMessage,
                succeeded: (<any>responseMessage).outcome === "Success"
            };
            delete result.type;
            delete result.outcome;
            let event: OutputEvent = {};
            if (result.exception) {
                event.exception = result.exception;
            }
            if (result.output) {
                event.stdout = result.output;
            }
            resultChannel.next(event);
            return result;
        });

        ret.then((_result) => {
            this.state = executionServiceStates.ready;
            this.currentRun = null;
        }).catch((_error) => {
            this.state = executionServiceStates.ready;
            this.currentRun = null;
            if(serviceErrorChannel !== null && serviceErrorChannel !== undefined && isApiMessageOfType(_error as ApiMessage, SERVICE_ERROR_RESPONSE)){
                serviceErrorChannel.next(<ServiceError>_error)
            }
        });

        this.state = executionServiceStates.running;

        this.currentRun = {
            requestId,
            request: ret
        };

        this.messageBus.post(request);
        return ret;
    }

    public subscribe(observer?: PartialObserver<OutputEvent>): Unsubscribable;
    public subscribe(next?: (value: OutputEvent) => void, error?: (error: any) => void, complete?: () => void): Unsubscribable;
    public subscribe(next?: any, error?: any, complete?: any) {
        return this.outputEventChannel.subscribe(next, error, complete);
    }
}