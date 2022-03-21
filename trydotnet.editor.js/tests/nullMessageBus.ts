// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from "rxjs";
import { IMessageBus } from "../src/messageBus";
import * as messages from "../src/messages";

export class NullMessageBus implements IMessageBus {

    private _messages: rxjs.Subject<messages.AnyApiMessage> = new rxjs.Subject<messages.AnyApiMessage>();
    private _onPostMessage: (message: messages.AnyApiMessage) => void;

    constructor() {

    }

    public postMessage(message: messages.AnyApiMessage): void {
        this._onPostMessage(message);
    }

    public get messages(): rxjs.Observable<messages.AnyApiMessage> {
        return this._messages;
    }
}