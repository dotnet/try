// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "../../src/internals/messageBus";
import { Subject, PartialObserver, Unsubscribable } from "rxjs";
import { ApiMessage } from "../../src/internals/apiMessages";

export class FakeMessageBus implements IMessageBus {

    constructor(private busId: string) {
    }

    public channel = new Subject<ApiMessage>();

    dispose(): void {
    }

    post(message: ApiMessage): void {
        this.channel.next(message);
    }

    subscribe(observer?: PartialObserver<ApiMessage>): Unsubscribable;
    subscribe(next?: (value: ApiMessage) => void, error?: (error: any) => void, complete?: () => void): Unsubscribable;
    subscribe(next?: any, error?: any, complete?: any): Unsubscribable {
        return this.channel.subscribe(next, error, complete);
    }

    id(): string {
        return this.busId;
    }
}
