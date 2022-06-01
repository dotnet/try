// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


// run api
export const RUN_REQUEST = 'run';
export const RUN_RESPONSE = 'RunCompleted';
export const RUN_STARTED_EVENT = 'RunStarted';
export const RUN_COMPLETED_EVENT = 'RunCompleted';
export const SERVICE_ERROR_RESPONSE = 'ServiceError';


// events
export const MONACO_READY_EVENT = 'MonacoEditorReady';
export const HOST_LISTENER_READY_EVENT = 'HostListenerReady';
export const HOST_EDITOR_READY_EVENT = 'HostEditorReady';
export const HOST_RUN_READY_EVENT = 'HostRunReady';
export const NOTIFY_HOST_RUN_BUSY = 'HostRunBusy';

// focus api
export const FOCUS_EDITOR_REQUEST = 'focusEditor';
export const SHOW_EDITOR_REQUEST = 'showEditor';



export type ApiMessage =
    {
        type: typeof RUN_REQUEST,
        requestId: string,
        parameters?: { [key: string]: any }
    } | {
        type: typeof RUN_RESPONSE,
        requestId: string,
        outcome: 'Success' | 'Exception' | 'CompilationError',
        [key: string]: any
    } | {
        type: typeof FOCUS_EDITOR_REQUEST
    } | {
        type: typeof SHOW_EDITOR_REQUEST
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