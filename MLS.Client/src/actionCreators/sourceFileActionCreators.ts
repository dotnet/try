// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { ThunkDispatch } from "redux-thunk";
import IState from "../IState";
import { AnyAction } from "redux";
import { SourceFile, CreateRegionsFromFilesRequest, CreateRegionsFromFilesResponse } from "../clientApiProtocol";

export function createRegionsFromProjectFiles(requestId: string, files: SourceFile[]) {
    return async (dispatch: ThunkDispatch<IState, void, AnyAction>, getState: () => IState): Promise<Action> => {
        let request: CreateRegionsFromFilesRequest = {
            requestId: requestId,
            files: files
        };

        const state = getState();
        const client = state.config.client;
        const applicationInsightsClient = state.config.applicationInsightsClient;

        try {
            var response = await client.createRegionsFromProjectFiles(request);
            return dispatch(createRegionsFromProjectFilesSuccess(response));
        }
        catch (ex) {
            if (applicationInsightsClient) {
                applicationInsightsClient.trackException(ex, "generateRegionsFromFiles", { "requestBody": JSON.stringify(request) });
            }
            return dispatch(createRegionsFromProjectFilesFailure(ex, request.requestId));
        }
    };
}

export function createRegionsFromProjectFilesSuccess(response: CreateRegionsFromFilesResponse): Action {
    let responseAcion: Action = {
        type: types.CREATE_REGIONS_FROM_SOURCEFILES_SUCCESS,
        ...response
    };

    return responseAcion;
}

export function createRegionsFromProjectFilesFailure(ex: Error, requestId: string): AnyAction {
    let responseAcion: Action = {
        type: types.CREATE_REGIONS_FROM_SOURCEFILES_FAILURE,
        requestId: requestId,
        error: ex
    };
    return responseAcion;
}
