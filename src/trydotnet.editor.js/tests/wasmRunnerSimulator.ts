// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as wasmRunner from "../src/wasmRunner";
import * as dotnetInteractive from "@microsoft/dotnet-interactive";
import * as rx from 'rxjs';

interface ISimulatorConfiguration {
    requests: {
        assembly: string;
        events: { source: string, value: string }[];
    }[]
}

export function createWasmRunnerSimulator(configurationPath?: string): wasmRunner.IWasmRunner {
    const configuration: ISimulatorConfiguration = configurationPath ? require(configurationPath) : undefined;
    let simulator = new Simulator(configuration);
    let runner: wasmRunner.IWasmRunner = (request) => {
        return simulator.processRequest(request);
    };
    return runner;
}



class Simulator {
    constructor(private _configuration?: ISimulatorConfiguration) { }

    public async processRequest(request: {
        assembly?: dotnetInteractive.Base64EncodedAssembly,
        onOutput: (output: string) => void,
        onError: (error: string) => void,
    }): Promise<void> {
        if (this._configuration) {
            let requestConfiguration = this._configuration.requests.find(r => r.assembly === request.assembly.value);
            if (requestConfiguration) {
                let events = requestConfiguration.events;
                for (let i = 0; i < events.length; i++) {
                    let event = events[i];
                    switch (event.source) {
                        case "stdOut":
                            request.onOutput(event.value);
                            break;
                        case "stdErr":
                            request.onError(event.value);
                            break;
                    }
                }
            }
            else {
                throw new Error(`No configuration found for assembly: ${request.assembly}`);
            }
        }
        else {
            throw new Error("Not configration available");
        }
    }
}
