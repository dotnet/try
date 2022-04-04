// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Subscribable, PartialObserver, Unsubscribable, Subject } from "rxjs";
import {
    ApiMessage,
    HOST_EDITOR_READY_EVENT,
    CODE_CHANGED_EVENT
} from "./apiMessages";
import { extractTargetOriginFromIFrame } from "./urlHelpers";
import {
    isNullOrUndefinedOrWhitespace,
    isNullOrUndefined
} from "../stringExtensions";

export function tryGetEditorId(
    iframe: HTMLIFrameElement,
    defaultEditorId: string
): string {
    if (!iframe) {
        throw new Error("iframe cannot be null");
    }

    let editorId = iframe.dataset.trydotnetEditorId;
    if (isNullOrUndefinedOrWhitespace(editorId)) {
        editorId = defaultEditorId;
    }

    return editorId;
}
export interface IMessageBus extends Subscribable<ApiMessage> {
    dispose(): void;
    post(message: ApiMessage): void;
    id(): string;
}

export class IFrameMessageBus implements IMessageBus {
    private targetOrigin: string;
    private internalChannel: Subject<ApiMessage>;
    private processMessageEvent: (event: any) => void;
    private isConnected: boolean = false;

    constructor(
        private iframe: HTMLIFrameElement,
        private window: Window,
        private messageBusId: string
    ) {
        this.processMessageEvent = ((event: any): void => {
            if (event.data && event.data.type) {
                let message = <ApiMessage>event.data;
                switch (message.type) {
                    case HOST_EDITOR_READY_EVENT:
                    case CODE_CHANGED_EVENT:
                        if (
                            isNullOrUndefined(message.editorId) ||
                            message.editorId === this.messageBusId
                        ) {
                            this.internalChannel.next(message);
                        }
                        break;
                    default:
                        this.internalChannel.next(message);
                        break;
                }
            }
        }).bind(this);

        this.internalChannel = new Subject<ApiMessage>();

        if (!this.window) {
            this.window = this.iframe.contentWindow.parent;
        }

        this.window.addEventListener("message", this.processMessageEvent);
    }

    public subscribe(observer?: PartialObserver<ApiMessage>): Unsubscribable;
    public subscribe(
        next?: (value: ApiMessage) => void,
        error?: (error: any) => void,
        complete?: () => void
    ): Unsubscribable;
    public subscribe(next?: any, error?: any, complete?: any): Unsubscribable {
        return this.internalChannel.subscribe(next, error, complete);
    }

    public dispose(): void {
        this.iframe.contentWindow.parent.removeEventListener(
            "message",
            this.processMessageEvent
        );
        this.internalChannel.complete();
    }

    public post(message: ApiMessage): void {
        if (!this.targetOrigin) {
            this.targetOrigin = extractTargetOriginFromIFrame(this.iframe);
        }
        this.iframe.contentWindow.postMessage(message, this.targetOrigin);
    }

    public id(): string {
        return this.messageBusId;
    }

    public targetIframe(): HTMLIFrameElement {
        return this.iframe;
    }
}
