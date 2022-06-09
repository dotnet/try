// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "../../src/internals/messageBus";
import { Subject, Unsubscribable, Observer } from "rxjs";
import { ApiMessage } from "../../src/apiMessages";

export class FakeMessageBus implements IMessageBus {

    private _requests = new Subject<{
        type: string; requestId?: string;[key: string]: any
    }>();

    private _responses = new Subject<{
        type: string; requestId?: string;[key: string]: any
    }>();
    constructor(private busId: string) {
    }
    subscribe(observer: Partial<Observer<{
        type: string; requestId?: string;[key: string]: any
    }>>): Unsubscribable {
        return this._responses.subscribe(observer);
    }

    dispose(): void {
    }

    post(message: {
        type: string; requestId?: string;[key: string]: any
    }): void {
        this._requests.next(message);
    }

    postResponse(message: {
        type: string; requestId?: string;[key: string]: any
    }): void {
        this._responses.next(message);
    }

    public get requests(): Subject<{
        type: string; requestId?: string;[key: string]: any
    }> {
        return this._requests;
    }

    // subscribe(observer?: PartialObserver<{ type: string, requestId?: string }>): Unsubscribable;
    // subscribe(next?: (value: { type: string, requestId?: string }) => void, error?: (error: any) => void, complete?: () => void): Unsubscribable;
    // subscribe(next?: any, error?: any, complete?: any): Unsubscribable {
    //     return this.channel.subscribe({ next, error, complete });
    // }

    id(): string {
        return this.busId;
    }
}
