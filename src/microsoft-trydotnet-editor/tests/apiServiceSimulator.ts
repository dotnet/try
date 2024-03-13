// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as apiService from "../src/apiService";
import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";

interface ISimulatorConfiguration {
    requests: {
        commands: polyglotNotebooks.KernelCommandEnvelopeModel[];
        events: polyglotNotebooks.KernelEventEnvelopeModel[];
    }[]
}
export function createApiServiceSimulator(configurationPath?: string): apiService.IApiService {
    const configuration: ISimulatorConfiguration = configurationPath ? require(configurationPath) : undefined;
    let simulator = new Simulator(configuration);
    let service: apiService.IApiService = (commands) => {
        return simulator.processRequest(commands);
    };

    return service;
}

class Simulator {

    constructor(private _configuration?: ISimulatorConfiguration) { }

    public async processRequest(commands: polyglotNotebooks.KernelCommandEnvelope[]): Promise<polyglotNotebooks.KernelEventEnvelope[]> {
        if (this._configuration) {
            let commandModels = commands.map(command => command.toJson());
            let requestConfiguration = this._configuration.requests.find(request => areEquivalentCommandSequences(commandModels, request.commands));
            if (requestConfiguration) {
                let events = patchEvents(requestConfiguration.events, commands);
                return new Promise<polyglotNotebooks.KernelEventEnvelope[]>((resolve, _reject) => {
                    resolve(events);
                });
            }

            throw new Error(`No configuration found for command sequence: ${JSON.stringify(commands)}`);
        }

        throw new Error("Not configration available");
    }
}

function patchEvents(events: polyglotNotebooks.KernelEventEnvelopeModel[], commands: polyglotNotebooks.KernelCommandEnvelope[]): polyglotNotebooks.KernelEventEnvelope[] {
    let patchedEvents: polyglotNotebooks.KernelEventEnvelopeModel[] = [];
    for (let i = 0; i < events.length; i++) {
        let event = events[i];
        var sourceCommand = commands.find(command => command.commandType === event.command!.commandType);
        let patchedEvent = { ...event };
        patchedEvent.command!.token = sourceCommand!.getOrCreateToken();

        patchedEvents.push(patchedEvent);
    }

    const eventsToReturn = patchedEvents.map(patchedEvent => polyglotNotebooks.KernelEventEnvelope.fromJson(patchedEvent));
    return eventsToReturn;
}


function areEquivalentCommandSequences(actual: polyglotNotebooks.KernelCommandEnvelopeModel[], expected: polyglotNotebooks.KernelCommandEnvelopeModel[]): boolean {
    if (actual.length !== expected.length) {
        return false;
    }

    for (let i = 0; i < actual.length; i++) {
        if (!areEquivalentCommands(actual[i], expected[i])) {
            return false;
        }
    }

    return true;
}

function areEquivalentCommands(actual: polyglotNotebooks.KernelCommandEnvelopeModel, expected: polyglotNotebooks.KernelCommandEnvelopeModel): boolean {
    if (actual.commandType !== expected.commandType) {
        return false;
    }

    // if (actual.token && actual.token !== expected.token) {
    //     return false;
    // }

    if (actual.command) {
        for (let key in actual.command) {
            if (actual.command.hasOwnProperty(key)) {
                const leftValue = actual.command[key];
                const rightValue = expected.command[key];
                if ((leftValue === null || leftValue === undefined) && (rightValue === null || rightValue === undefined)) {
                    continue;
                }

                const left = JSON.stringify(leftValue);
                const right = JSON.stringify(rightValue);
                if (left !== right) {
                    return false;
                }
            }
        }
    }

    return true;
}

