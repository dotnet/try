// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RunConfiguration, RunResult, OutputEvent, ServiceError } from "../session";
import { ApiMessage, RUN_REQUEST, RUN_RESPONSE, SERVICE_ERROR_RESPONSE } from "../apiMessages";
import { responseFor } from "./responseFor";
import { IMessageBus } from "./messageBus";
import { RequestIdGenerator } from "./requestIdGenerator";
import { Subject, Subscribable, PartialObserver, Unsubscribable, Observer } from "rxjs";
import * as newContract from "../newContract";

enum executionServiceStates {
    ready,
    running
}
export class executionService implements Subscribable<OutputEvent> {
    private state: executionServiceStates = executionServiceStates.ready;
    private outputEventChannel: Subject<OutputEvent> = new Subject<OutputEvent>();
    private currentRun: { requestId: string, request: Promise<RunResult> };

    constructor(private messageBus: IMessageBus, private requestIdGenerator: RequestIdGenerator, private serviceErrorChannel: Observer<ServiceError>) {
        if (!this.messageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }
    }

    public async run(configuration?: RunConfiguration): Promise<RunResult> {

        const requestId = await this.requestIdGenerator.getNewRequestId();

        if (this.state == executionServiceStates.running) {
            return this.currentRun.request;
        }

        let resultChannel = this.outputEventChannel;
        let serviceErrorChannel = this.serviceErrorChannel;

        this.currentRun = {
            requestId,
            request: responseFor<RunResult>(this.messageBus, RUN_RESPONSE, requestId, (responseMessage) => {
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
            })
        }; /*? $ */

        this.state = executionServiceStates.running;

        this.currentRun.request.then((_result) => {
            this.currentRun = null;
            this.state = executionServiceStates.ready;
        }).catch((_error) => {

            this.currentRun = null;
            this.state = executionServiceStates.ready;
            if (serviceErrorChannel !== null && serviceErrorChannel !== undefined && newContract.isMessageOfType(_error as ApiMessage, SERVICE_ERROR_RESPONSE)) {
                serviceErrorChannel.next(<ServiceError>_error)
            }
        });


        let request: ApiMessage = {
            type: RUN_REQUEST,
            requestId: requestId
        }

        if (configuration) {
            request.parameters = JSON.parse(JSON.stringify(configuration));
        }
        this.messageBus.post(request);

        return this.currentRun.request;
    }

    public subscribe(observer?: PartialObserver<OutputEvent>): Unsubscribable;
    public subscribe(next?: (value: OutputEvent) => void, error?: (error: any) => void, complete?: () => void): Unsubscribable;
    public subscribe(next?: any, error?: any, complete?: any) {
        return this.outputEventChannel.subscribe(next, error, complete);
    }
}