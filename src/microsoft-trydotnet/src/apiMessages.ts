// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


// run api
export const RUN_REQUEST = "run";
export const RUN_RESPONSE = "RunCompleted";
export const RUN_STARTED_EVENT = "RunStarted";
export const RUN_COMPLETED_EVENT = "RunCompleted";
export const SERVICE_ERROR_RESPONSE = "ServiceError";



// theme api
export const CONFIGURE_MONACO_REQUEST = "configureMonacoEditor";
export const DEFINE_THEMES_REQUEST = "defineMonacoEditorThemes";

// events 
export const MONACO_READY_EVENT = "MonacoEditorReady";
export const HOST_EDITOR_READY_EVENT = "HostEditorReady";
export const HOST_RUN_READY_EVENT = "HostRunReady";

// focus api
export const FOCUS_EDITOR_REQUEST = "focusEditor";
export const SHOW_EDITOR_REQUEST = "showEditor";

// workspace api
export const SET_WORKSPACE_REQUEST = "setWorkspace";
export const SET_EDITOR_CODE_REQUEST = "setSourceCode";
export const GET_EDITOR_CODE_REQUEST = "getEditorSourceCode";
export const GET_EDITOR_CODE_RESPONSE = "editorSourceCode";



export function isMessageOfType(message: { type: string }, type: string): boolean {
    return message && message.type && type && message.type.toLowerCase() === type.toLowerCase();
}

export function isMessageCorrelatedTo(message: { requestId?: string }, requestId: string): boolean {
    return message && requestId && message.requestId && message.requestId === requestId;
}

export type ApiMessage =
    {
        type: typeof RUN_REQUEST,
        requestId: string,
        parameters?: { [key: string]: any }
    } | {
        type: typeof RUN_RESPONSE,
        requestId: string,
        outcome: "Success" | "Exception" | "CompilationError",
        [key: string]: any
    } | {
        type: typeof CONFIGURE_MONACO_REQUEST,
        [key: string]: any
    } | {
        type: typeof DEFINE_THEMES_REQUEST,
        themes: any
    } | {
        type: typeof FOCUS_EDITOR_REQUEST
    } | {
        type: typeof SHOW_EDITOR_REQUEST
    } | {
        type: typeof SET_WORKSPACE_REQUEST,
        workspace: any,
        bufferId: string,
        requestId: string
    } | {
        type: typeof SET_EDITOR_CODE_REQUEST,
        sourceCode: string,
        requestId: string
    } | {
        type: typeof HOST_EDITOR_READY_EVENT,
        editorId?: string
    } | {
        type: typeof HOST_RUN_READY_EVENT,
        editorId?: string
    } | {
        type: typeof SERVICE_ERROR_RESPONSE,
        statusCode: string,
        message: string,
        requestId: string,
    };