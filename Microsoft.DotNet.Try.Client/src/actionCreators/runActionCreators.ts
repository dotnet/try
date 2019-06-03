// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { ThunkDispatch } from "redux-thunk";
import IMlsClient, { IRunResponse, IRunRequest, ICompileResponse } from "../IMlsClient";
import IState, { IDiagnostic } from "../IState";
import { AnyAction } from "redux";
import { IApplicationInsightsClient } from "../ApplicationInsights";
import { newOperationId } from "../utilities/requestIdGenerator";

export function runClicked() {
    return {
        type: types.RUN_CLICKED
    };
}

export function runFailure(ex: any, requestId?: string): any {
    let response = {
        type: types.RUN_CODE_FAILURE,
        error: {
            statusCode: ex.statusCode,
            message: ex.message,
            requestId: requestId ? requestId : ex.requestId
        }
    };

    return response;
}

export function compileRequest(requestId: string, workspaceVersion: number): Action {
    return {
        type: types.COMPILE_CODE_REQUEST,
        requestId: requestId,
        workspaceVersion: workspaceVersion
    };
}

export function compileFailure(response: ICompileResponse, workspaceVersion: number): Action {
    let result: Action = {
        type: types.COMPILE_CODE_FAILURE,
        diagnostics: response.diagnostics,
        workspaceVersion
    };

    if (response.requestId) {
        result.requestId = response.requestId;
    }
    return result;
}

export function compileSuccess(response: ICompileResponse, workspaceVersion: number): Action {
    let result: Action = {
        type: types.COMPILE_CODE_SUCCESS,
        base64assembly: response.base64assembly,
        workspaceVersion
    };

    if (response.requestId) {
        result.requestId = response.requestId;
    }
    return result;
}

export function runRequest(requestId: string): Action {
    return {
        type: types.RUN_CODE_REQUEST,
        requestId: requestId
    };
}

export function runSuccess(response: IRunResponse, executionStrategy: "Agent" | "Blazor" = "Agent"): Action {
    let result: Action = {
        type: types.RUN_CODE_SUCCESS,
        exception: response.exception,
        output: response.output,
        succeeded: response.succeeded,
        diagnostics: response.diagnostics,
        instrumentation: response.instrumentation,
        variableLocations: response.variableLocations,
        executionStrategy: executionStrategy
    };

    if (response.requestId) {
        result.requestId = response.requestId;
    }

    return result;
}

export function outputUpdated(newOutput: string[]): Action {
    return {
        type: types.OUTPUT_UPDATED,
        output: newOutput
    };
}

function sendBlazorMessage<TResult>(payload: object, callback: (arg: TResult) => void): Action {
    return {
        type: types.SEND_BLAZOR_MESSAGE,
        callback: callback,
        payload: payload
    };
}

export function blazorReady(editorId: string): Action {
    return {
        type: types.BLAZOR_READY,
        editorId: editorId
    };
}

export function run(requestId?: string, parameters: { [key: string]: any } = {}) {
    return async (dispatch: ThunkDispatch<IState, void, AnyAction>, getState: () => IState): Promise<Action> => {
        const state = getState();

        const requestIdentifier = requestId ? requestId : newOperationId();

        const request: IRunRequest = {
            ...parameters,
            workspace: state.workspace.workspace,
            activeBufferId: state.monaco.bufferId,
            requestId: requestIdentifier
        };

        dispatch(runRequest(requestIdentifier));

        if (state.config.useLocalCodeRunner) {
            return runUsingBlazor(state, request, parameters, state.config.client, state.config.applicationInsightsClient, dispatch);
        }
        else {
            return runOnAgent(request, state.config.client, state.config.applicationInsightsClient, dispatch);
        }
    };
}

async function runOnAgent(request: IRunRequest, client: IMlsClient, applicationInsightsClient: IApplicationInsightsClient, dispatch: ThunkDispatch<IState, void, AnyAction>): Promise<Action> {
    try {
        var json = await client.run(request);
        return dispatch(runSuccess(json));
    }
    catch (ex) {
        if (applicationInsightsClient) {
            applicationInsightsClient.trackException(ex, "runOnAgent", { "requestBody": JSON.stringify(request) });
        }
        return dispatch(runFailure(ex, request.requestId));
    }
}

export interface WasmCodeRunnerResponse {
    exception?: string;
    output?: string[];
    succeeded?: boolean;
    diagnostics?: IDiagnostic[];
    runnerException?: string;
    codeRunnerVersion: string;
    [key: string]: any;
}

export interface WasmCodeRunnerRequest {
    diagnostics?: IDiagnostic[];
    base64assembly?: string;
    requestId?: string;
    succeeded?: boolean;
    [key: string]: any;
}

function isCompileResult(response: any): response is ICompileResponse {
    return response.base64assembly !== undefined;
}

async function runUsingBlazor(state: IState, request: IRunRequest, parameters: { [key: string]: any }, client: IMlsClient, applicationInsightsClient: IApplicationInsightsClient, dispatch: ThunkDispatch<IState, void, AnyAction>): Promise<Action> {
    let response: ICompileResponse;
    if (state.compile.workspaceVersion === state.workspace.sequenceNumber) {
        response = state.compile;
    }
    else {
        try {
            response = await client.compile(request);
        } catch (ex) {
            if (applicationInsightsClient) {
                applicationInsightsClient.trackException(ex, "Blazor.Error", { "requestBody": JSON.stringify(request) });
            }
            dispatch(compileFailure({
                succeeded: false,
                diagnostics: [],
                requestId: request.requestId
            }, state.workspace.sequenceNumber));
            return dispatch(runFailure(ex, request.requestId));
        }
    }

    if (!response.succeeded) {
        dispatch(compileFailure(response, state.workspace.sequenceNumber));
        let output: string[] = [];
        if (response.diagnostics) {
            output = response.diagnostics.map(d => d.message);
        }
        return dispatch(runSuccess({ succeeded: response.succeeded, output: output, diagnostics: response.diagnostics, requestId: request.requestId }, "Blazor"));
    }
    else {
        dispatch(compileSuccess(response, state.workspace.sequenceNumber));

        let wasmRunRequest: WasmCodeRunnerRequest = {
            ...response,
            ...parameters,
            requestId: request.requestId
        };

        return dispatch(sendBlazorMessage<WasmCodeRunnerResponse>(wasmRunRequest,
            (runResponse: WasmCodeRunnerResponse) => {
                if (isCompileResult(runResponse)) {
                    return;
                }

                if (runResponse.runnerException) {
                    applicationInsightsClient.trackEvent("Blazor.Error", { message: runResponse.runnerException, codeRunnerVersion: runResponse.codeRunnerVersion });
                }
                else {
                    applicationInsightsClient.trackEvent("Blazor.Success", { codeRunnerVersion: runResponse.codeRunnerVersion });
                }

                dispatch(runSuccess({ succeeded: runResponse.succeeded, output: runResponse.output, requestId: request.requestId }, "Blazor"));
            }));

    }
}

export function setDiagnostics(diagnostics: IDiagnostic[]) {
    return {
        type: types.SET_DIAGNOSTICS,
        diagnostics: diagnostics
    };
}
