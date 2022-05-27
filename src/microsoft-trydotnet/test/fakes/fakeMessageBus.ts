// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "../../src/internals/messageBus";
import { Subject, Unsubscribable, Observer } from "rxjs";
import { ApiMessage } from "../../src/apiMessages";

export class FakeMessageBus implements IMessageBus {

    constructor(private busId: string) {
    }
    subscribe(observer: Partial<Observer<{ type: string; requestId?: string; }>>): Unsubscribable {
        return this.channel.subscribe(observer);
    }

    public channel = new Subject<ApiMessage>();

    dispose(): void {
    }

    post(message: ApiMessage): void {
        this.channel.next(message);
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
