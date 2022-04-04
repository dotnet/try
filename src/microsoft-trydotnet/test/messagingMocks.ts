// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DOMWindow } from "jsdom";
import { Configuration } from "../src";
import { ApiMessage, RUN_REQUEST, COMPILE_REQUEST, CREATE_OPERATION_ID_REQUEST, CREATE_OPERATION_ID_RESPONSE, CREATE_REGIONS_FROM_SOURCEFILES_REQUEST, CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE, SET_EDITOR_CODE_REQUEST, CODE_CHANGED_EVENT, SET_ACTIVE_BUFFER_REQUEST, HOST_EDITOR_READY_EVENT, SET_WORKSPACE_REQUEST, HOST_RUN_READY_EVENT, } from "../src/internals/apiMessages";
import { SourceFileRegion, SourceFile } from "../src/project";
import { wait } from "./wait";

export type EditorState = {
    content: string,
    documentId: string
};

export function registerForRunRequest(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, onRequest: (request: ApiMessage) => ApiMessage): void {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        if (message.data.type === RUN_REQUEST) {
            let apiCall = <ApiMessage>(message.data);
            window.postMessage(onRequest(apiCall), configuration.hostOrigin);
        }
    });
}

export function registerForLongRunRequest(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, onRequest: (request: ApiMessage) => ApiMessage): void {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        if (message.data.type === RUN_REQUEST) {
            let apiCall = <ApiMessage>(message.data);
            wait(1000).then(() => {
                window.postMessage(onRequest(apiCall), configuration.hostOrigin);
            });            
        }
    });
}

export function registerForCompileRequest(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, onRequest: (request: ApiMessage) => ApiMessage): void {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        if (message.data.type === COMPILE_REQUEST) {
            let apiCall = <ApiMessage>(message.data);
            window.postMessage(onRequest(apiCall), configuration.hostOrigin);
        }
    });
}

export function registerForRequestIdGeneration(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, onRequest: (requestId: string) => string) {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        if (message.data.type === CREATE_OPERATION_ID_REQUEST) {
            let response: ApiMessage = {
                type: CREATE_OPERATION_ID_RESPONSE,
                requestId: message.data.requestId,
                operationId: onRequest(message.data.requestId)

            };
            window.postMessage(response, configuration.hostOrigin);
        }
    });
}

export function registerForRegionFromFile(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, onRequest: (files: SourceFile[]) => SourceFileRegion[]) {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        if (message.data.type === CREATE_REGIONS_FROM_SOURCEFILES_REQUEST) {
            let response: ApiMessage = {
                type: CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE,
                requestId: message.data.requestId,
                regions: onRequest(message.data.files)

            };
            window.postMessage(response, configuration.hostOrigin);
        }
    });
}

export function registerForEditorMessages(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, editorState: EditorState) {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        let apiMessage = <ApiMessage>(message.data);
        if (apiMessage.type === SET_ACTIVE_BUFFER_REQUEST) {
            editorState.documentId = apiMessage.bufferId;
        }
        if (apiMessage.type === SET_EDITOR_CODE_REQUEST) {
            editorState.content = apiMessage.sourceCode;

            let response: ApiMessage = {
                type: CODE_CHANGED_EVENT,
                requestId: apiMessage.requestId,
                sourceCode: apiMessage.sourceCode,
                bufferId: editorState.documentId
            };

            window.postMessage(response, configuration.hostOrigin);
        }
    });
}

export function trackSetWorkspaceRequests(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, messageStack: ApiMessage[]) {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        let apiMessage = <ApiMessage>(message.data);
        if (apiMessage.type === SET_WORKSPACE_REQUEST) {
            messageStack.push(apiMessage);
        }
    });
}

export function raiseTextChange(configuration: Configuration, window: DOMWindow, newText: string, documentId: string) {
    let message = {
        type: CODE_CHANGED_EVENT,
        sourceCode: newText,
        bufferId: documentId
    };

    window.postMessage(message, configuration.hostOrigin);
}

export function notifyEditorReady(configuration: Configuration, window: DOMWindow) {
    let response: ApiMessage = {
        type: HOST_EDITOR_READY_EVENT
    };

    window.postMessage(response, configuration.hostOrigin);
}

export function notifyEditorReadyWithId(configuration: Configuration, window: DOMWindow, editorId: string) {
    let response: ApiMessage = {
        type: HOST_EDITOR_READY_EVENT,
        editorId: editorId
    };

    window.postMessage(response, configuration.hostOrigin);
}

export function notifyRunReadyWithId(configuration: Configuration, window: DOMWindow, editorId: string) {
    let response: ApiMessage = {
        type: HOST_RUN_READY_EVENT,
        editorId: editorId
    };

    window.postMessage(response, configuration.hostOrigin);
}