// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DOMWindow } from "jsdom";
import { Configuration } from "../src";
import { ApiMessage, RUN_REQUEST, SET_EDITOR_CODE_REQUEST, HOST_EDITOR_READY_EVENT, SET_WORKSPACE_REQUEST, HOST_RUN_READY_EVENT, } from "../src/apiMessages";
import { SourceFile } from "../src/project";
import { wait } from "./wait";
import * as newContract from "../src/newContract";
import * as dotnetInteractive from "@microsoft/dotnet-interactive";
import { DocumentId } from "../src/internals/document";

export type EditorState = {
    content: string,
    documentId: DocumentId
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

export function registerForSetWorkspace(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, onRequest: (files: SourceFile[]) => dotnetInteractive.ProjectItem[]) {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        message.data;//?
        if (message.data.type === SET_WORKSPACE_REQUEST) {

            let response: newContract.ProjectOpened = {
                type: dotnetInteractive.ProjectOpenedType,
                requestId: message.data.requestId,
                editorId: message.data.editorId,
                projectItems: onRequest(message.data.workspace.files.map((f: { text: string; name: string; }) => ({ content: f.text, name: f.name })))

            };
            window.postMessage(response, configuration.hostOrigin);
        }
    });
}

export function registerForEditorMessages(configuration: Configuration, iframe: HTMLIFrameElement, window: DOMWindow, editorState: EditorState) {
    iframe.contentWindow.addEventListener("message", (message: any) => {
        let apiMessage = <{ type: string }>(message.data);//?
        if (apiMessage.type === dotnetInteractive.OpenDocumentType) {
            editorState.documentId = new DocumentId(<newContract.OpenDocument>(message.data));//?
        }
        if (apiMessage.type === SET_EDITOR_CODE_REQUEST) {
            const request = <{ sourceCode: string }>message.data;
            editorState.content = request.sourceCode;
            editorState;//?
            let response: newContract.EditorContentChanged = {
                type: newContract.EditorContentChangedType,
                content: request.sourceCode,
                relativeFilePath: editorState.documentId.relativeFilePath,
                regionName: editorState.documentId.regionName,
                editorId: (<any>apiMessage).editorId,
            };//?

            window.postMessage(response, configuration.hostOrigin);
        }
    });
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
    };//?

    window.postMessage(response, configuration.hostOrigin);
}

export function notifyRunReadyWithId(configuration: Configuration, window: DOMWindow, editorId: string) {
    let response: ApiMessage = {
        type: HOST_RUN_READY_EVENT,
        editorId: editorId
    };

    window.postMessage(response, configuration.hostOrigin);
}