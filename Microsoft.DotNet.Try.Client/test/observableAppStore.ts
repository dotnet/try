// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Action } from "../src/constants/ActionTypes";
import IMlsClient from "../src/IMlsClient";
import MlsClientSimulator from "./mlsClient/mlsClient.simulator";
import actions from "../src/actionCreators/actions";
import appStore from "../src/app.store";
import enableActionRecorder from "./ActionRecorderMiddleware";
import { IStore } from "../src/IStore";
import { IApplicationInsightsClient } from "../src/ApplicationInsights";

export interface IObservableAppStore extends IStore {
    getActions: () => Action[];
    configure: (actions: any[]) => void;
    withClient: (client: IMlsClient) => IObservableAppStore;
    withDefaultClient: () => IObservableAppStore;
    withAiClient: (client: IApplicationInsightsClient) => IObservableAppStore;
}

const getStore = () => {
    var recordedActions: any[] = [];
    var store: IObservableAppStore = {
        ...appStore([enableActionRecorder(recordedActions)]),
        getActions: () => { return recordedActions; },
        configure: (configActions = []) => {
            configActions.forEach((action) => {
                store.dispatch(action);
            });

            recordedActions.length = 0;
        },
        withClient: (client) => {
            client = client || new MlsClientSimulator();
            store.configure([actions.setClient(client)]);
            return store;
        },
        withDefaultClient: () => {
            var client = new MlsClientSimulator();
            store.configure([actions.setClient(client)]);
            return store;
        },
        withAiClient: (client) => {
            store.configure([actions.enableClientTelemetry(client)]);
            return store;
        }
    };

    return store;
};

export default getStore;
