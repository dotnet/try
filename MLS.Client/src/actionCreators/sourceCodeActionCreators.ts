// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";

import { Dispatch, AnyAction, ActionCreator, Action } from "redux";
import IMlsClient from "../IMlsClient";
import IState, { IWorkspace } from "../IState";
import { getBufferContent } from "../workspaces";
import { LoadWorkspaceFromGist } from "./workspaceActionCreators";
import { ThunkAction, ThunkDispatch } from "redux-thunk";

export function loadCodeRequest(from: string): AnyAction {
    return {
        type: types.LOAD_CODE_REQUEST as typeof types.LOAD_CODE_REQUEST,
        from: from
    };
}

export function loadCodeSuccess(sourceCode: string, bufferId?: string) {
    return {
        type: types.LOAD_CODE_SUCCESS as typeof types.LOAD_CODE_SUCCESS,
        sourceCode,
        bufferId
    };
}

export function loadCodeFailure(ex: Error) {
    return {
        type: types.LOAD_CODE_FAILURE,
        ex
    };
}

export function setAdditionalUsings(usings: string[]): AnyAction {
    return {
        type: types.SET_ADDITIONAL_USINGS as typeof types.SET_ADDITIONAL_USINGS,
        additionalUsings: usings
    };
}

const loadCodeFromConfig = (dispatch: Dispatch<Action>, sourceCode: string, bufferId: string) => dispatch(loadCodeSuccess(sourceCode, bufferId));

const loadCodeFromUri = async function (dispatch: Dispatch<AnyAction>, mlsClient: IMlsClient, sourceUri: string, bufferId: string) {
    try {
        var response = await mlsClient.getSourceCode({ sourceUri });
        return dispatch(loadCodeSuccess(response.buffer, bufferId));
    }
    catch (ex) {
        dispatch(loadCodeFailure(ex));
    }
};

const loadCodeFromWorkspace = 
    (dispatch: Dispatch<Action>, workspace: IWorkspace, bufferId: string) =>
        dispatch(loadCodeSuccess(extractCodeFromWorkspace(workspace, bufferId), bufferId));


function extractCodeFromWorkspace(workspace: IWorkspace, bufferId: string): string {    
    return getBufferContent(workspace, bufferId);
}

export const loadSource: ActionCreator<ThunkAction<Promise<Action>, IState, void, AnyAction>> =
    () => 
        async (dispatch: ThunkDispatch<IState, void, Action>, getState: () => IState): Promise<Action> => {
        const state = getState();
        const from = state.config.from;
        const bufferId = state.monaco.bufferId;
        const gitHubPanelEnabled = state.ui.canShowGitHubPanel;

        dispatch(loadCodeRequest(from));

        if (from.startsWith("gist::")) {
            const workspaceType = state.workspace.workspace.workspaceType;
            const gistId = from.replace("gist::", "");
            return dispatch(LoadWorkspaceFromGist(gistId, bufferId, workspaceType, gitHubPanelEnabled));
        }
        switch (from) {
            case "default":
                return Promise.resolve(loadCodeFromWorkspace(dispatch, state.config.defaultWorkspace, bufferId));
            case "parameter":
                return Promise.resolve(loadCodeFromConfig(dispatch, state.config.defaultCodeFragment, bufferId));
            case "workspace":
                return Promise.resolve(loadCodeFromWorkspace(dispatch, state.workspace.workspace, bufferId));
            default:
                return loadCodeFromUri(dispatch, state.config.client, from, bufferId);
        }
    };
