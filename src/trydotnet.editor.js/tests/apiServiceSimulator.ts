// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as apiService from "../src/apiService";
import * as dotnetInteractive from "@microsoft/dotnet-interactive";

interface ISimulatorConfiguration {
    requests: {
        commands: dotnetInteractive.KernelCommandEnvelope[];
        events: dotnetInteractive.KernelEventEnvelope[];
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

    public async processRequest(commands: dotnetInteractive.KernelCommandEnvelope[]): Promise<dotnetInteractive.KernelEventEnvelope[]> {
        if (this._configuration) {
            let requestConfiguration = this._configuration.requests.find(request => areEquivalentCommandSequences(commands, request.commands));
            if (requestConfiguration) {
                let events = patchEvents(requestConfiguration.events, commands);
                return new Promise<dotnetInteractive.KernelEventEnvelope[]>((resolve, _reject) => {
                    resolve(events);
                });
            }

            throw new Error(`No configuration found for command sequence: ${JSON.stringify(commands)}`);
        }

        throw new Error("Not configration available");
    }
}

function patchEvents(events: dotnetInteractive.KernelEventEnvelope[], commands: dotnetInteractive.KernelCommandEnvelope[]): dotnetInteractive.KernelEventEnvelope[] {
    let patchedEvents: dotnetInteractive.KernelEventEnvelope[] = [];
    for (let i = 0; i < events.length; i++) {
        let event = events[i];
        var sourceCommand = commands.find(command => command.commandType === event.command.commandType);
        let patchedEvent = { ...event };
        patchedEvent.command.id = sourceCommand.id;
        patchedEvent.command.token = sourceCommand.token;

        patchedEvents.push(patchedEvent);
    }

    return patchedEvents;
}


function areEquivalentCommandSequences(actual: dotnetInteractive.KernelCommandEnvelope[], expected: dotnetInteractive.KernelCommandEnvelope[]): boolean {
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

function areEquivalentCommands(actual: dotnetInteractive.KernelCommandEnvelope, expected: dotnetInteractive.KernelCommandEnvelope): boolean {
    if (actual.commandType !== expected.commandType) {
        return false;
    }

    // if (actual.token && actual.token !== expected.token) {
    //     return false;
    // }

    if (actual.command) {
        for (let key in actual.command) {
            if (actual.command.hasOwnProperty(key)) {
                if (JSON.stringify(actual.command[key]) !== JSON.stringify(expected.command[key])) {
                    return false;
                }
            }
        }
    }

    return true;
}

