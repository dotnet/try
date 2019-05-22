// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { ThunkDispatch } from "redux-thunk";
import IState from "../IState";
import { AnyAction } from "redux";
import { CreateProjectFromGistRequest, CreateProjectResponse } from "../clientApiProtocol";

export function createProjectFromGist(requestId: string, packageName: string, gistId: string, commitHash?: string) {
    return async (dispatch: ThunkDispatch<IState, void, AnyAction>, getState: () => IState): Promise<Action> => {
        let request: CreateProjectFromGistRequest = {
            requestId: requestId,
            projectTemplate: packageName,
            gistId: gistId,
        };

        if (commitHash) {
            request.commitHash = commitHash;
        }
        const state = getState();
        const client = state.config.client;
        const applicationInsightsClient = state.config.applicationInsightsClient;

        try {
            var response = await client.createProjectFromGist(request);
            return dispatch(projectCreationSuccess(response));
        }
        catch (ex) {
            if (applicationInsightsClient) {
                applicationInsightsClient.trackException(ex, "createProjectFromGist", { "requestBody": JSON.stringify(request) });
            } 
            return dispatch(projectCreationFailure(ex, request.requestId));
        }
    };
}

export function projectCreationSuccess(response: CreateProjectResponse): Action {
    let responseAcion: Action = {
        type: types.CREATE_PROJECT_SUCCESS,
        ...response
    };

    return responseAcion;
}

export function projectCreationFailure(ex: Error, requestId: string): AnyAction {
    let responseAcion: Action = {
        type: types.CREATE_PROJECT_FAILURE,
        requestId: requestId,
        error: ex
    };
    return responseAcion;
}
