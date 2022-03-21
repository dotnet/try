// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as contract from './contract';

export type AnyApiMessage = {
    type: string;
    requestId?: string;
    messageOrigin?: string;
    [index: string]: any
};

// run api
export const RUN_REQUEST = 'run';
export const RUN_RESPONSE = 'RunCompleted';
export const RUN_STARTED_EVENT = 'RunStarted';
export const RUN_COMPLETED_EVENT = 'RunCompleted';
export const SERVICE_ERROR_RESPONSE = 'ServiceError';

// wasm api
export const RUN_WASM_REQUEST = 'Wasmrun';
export const RUN_WASM_RESPONSE = 'WasmRunCompleted';

// compile api
export const COMPILE_REQUEST = 'compile';
export const COMPILE_RESPONSE = 'compileCompleted';

// theme api
export const CONFIGURE_MONACO_REQUEST = 'configureMonacoEditor';
export const DEFINE_THEMES_REQUEST = 'defineMonacoEditorThemes';

// events
export const MONACO_READY_EVENT = 'MonacoEditorReady';
export const HOST_LISTENER_READY_EVENT = 'HostListenerReady';
export const HOST_EDITOR_READY_EVENT = 'HostEditorReady';
export const HOST_RUN_READY_EVENT = 'HostRunReady';

// focus api
export const FOCUS_EDITOR_REQUEST = 'focusEditor';
export const SHOW_EDITOR_REQUEST = 'showEditor';

// workspace api
export const SET_WORKSPACE_REQUEST = 'setWorkspace';
export const SET_EDITOR_CODE_REQUEST = 'setSourceCode';
export const GET_EDITOR_CODE_REQUEST = 'getEditorSourceCode';
export const GET_EDITOR_CODE_RESPONSE = 'editorSourceCode';
export const CODE_CHANGED_EVENT = 'CodeModified';
export const SET_ACTIVE_BUFFER_REQUEST = 'setActiveBufferId';

// operationId api
export const CREATE_OPERATION_ID_REQUEST = 'generateoperationid';
export const CREATE_OPERATION_ID_RESPONSE = 'operationidgenerated';

// region api
export const CREATE_REGIONS_FROM_SOURCEFILES_REQUEST = 'generateregionfromfiles';
export const CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE = 'generateregionfromfilesresponse';

// project api
export const CREATE_PROJECT_RESPONSE = 'createprojectresponse';

export type ApiMessage =
    {
        type: typeof CREATE_PROJECT_RESPONSE,
        requestId: string,
        success: boolean,
        project?: Project,
        error?: any
    } | {
        type: typeof CREATE_REGIONS_FROM_SOURCEFILES_REQUEST,
        requestId: string,
        files: SourceFile[]
    } | {
        type: typeof CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE,
        requestId: string,
        regions: SourceFileRegion[]
    } | {
        type: typeof CREATE_OPERATION_ID_REQUEST,
        requestId: string
    } | {
        type: typeof CREATE_OPERATION_ID_RESPONSE,
        requestId: string,
        operationId: string
    } | {
        type: typeof RUN_REQUEST,
        requestId: string,
        parameters?: { [key: string]: any }
    } | {
        type: typeof RUN_RESPONSE,
        requestId: string,
        outcome: 'Success' | 'Exception' | 'CompilationError',
        [key: string]: any
    } | {
        type: typeof COMPILE_REQUEST,
        requestId: string,
        parameters?: { [key: string]: any }
    } | {
        type: typeof COMPILE_RESPONSE,
        requestId: string,
        outcome: 'Success' | 'Exception' | 'CompilationError',
        [key: string]: any
    } | {
        type: typeof CONFIGURE_MONACO_REQUEST,
        theme: string
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
        type: typeof CODE_CHANGED_EVENT,
        sourceCode: string,
        requestId: string,
        bufferId: string,
        editorId?: string
    } | {
        type: typeof SET_ACTIVE_BUFFER_REQUEST,
        bufferId: string,
        requestId: string
    } | {
        type: typeof HOST_LISTENER_READY_EVENT,
        editorId?: string
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

export function isApiMessageOfType(message: ApiMessage, type: string): boolean {
    return message && message.type && type && message.type.toLowerCase() === type.toLowerCase();
}

export function isApiMessageCorrelatedTo(message: ApiMessage, requestId: string): boolean {
    return message && requestId && (<any>message).requestId && (<any>message).requestId === requestId;
}

export type Project = {
    package: string,
    packageVersion?: string,
    language?: string,
    files: SourceFile[],
    [key: string]: any
};

export type SourceFile = {
    name: string,
    content: string,
};

export type SourceFileRegion = {
    id: string,
    content: string,
};