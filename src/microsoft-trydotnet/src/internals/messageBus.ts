// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Subscribable, Unsubscribable, Subject, Observer } from "rxjs";
import { ApiMessage } from "../apiMessages";
import { isNullOrUndefinedOrWhitespace } from "../stringExtensions";

export interface IMessageBus extends Subscribable<{ type: string, requestId?: string }> {
    dispose(): void;
    post(message: ApiMessage): void;
}

export class IFrameMessageBus implements IMessageBus {
    private targetOrigin: string;
    private internalChannel: Subject<{ type: string, requestId?: string }>;
    private processMessageEvent: (event: any) => void;

    constructor(
        private iframe: HTMLIFrameElement,
        private window: Window
    ) {
        this.internalChannel = new Subject<{ type: string, requestId?: string }>();
        this.processMessageEvent = ((event: any): void => {
            if (event.data && event.data.type) {
                let message = <{ type: string, requestId?: string }>event.data;//?
                this.internalChannel.next(message);
            }
        }).bind(this);


        if (!this.window) {
            this.window = this.iframe.contentWindow.parent;
        }

        this.window.addEventListener("message", this.processMessageEvent);
    }
    subscribe(observer: Partial<Observer<{ type: string, requestId?: string }>>): Unsubscribable {
        return this.internalChannel.subscribe(observer);
    }

    public dispose(): void {
        this.iframe.contentWindow.parent.removeEventListener(
            "message",
            this.processMessageEvent
        );
        this.internalChannel.complete();
    }

    public post(message: { type: string, requestId?: string }): void {
        if (!this.targetOrigin) {
            this.targetOrigin = "*";// extractTargetOriginFromIFrame(this.iframe);
        }
        message;//?
        this.targetOrigin;//?
        this.iframe.contentWindow.postMessage(message, this.targetOrigin);
    }

    public targetIframe(): HTMLIFrameElement {
        return this.iframe;
    }
}
