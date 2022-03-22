// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import * as messages from './messages';

export interface IMessageBus {
    postMessage(message: messages.AnyApiMessage): void;
    messages: rxjs.Observable<messages.AnyApiMessage>;
}

export class MessageBus implements IMessageBus {
  constructor (private _onPostMessage: (message: messages.AnyApiMessage) => void, private _messages: rxjs.Observable<messages.AnyApiMessage>) { }

  public postMessage (message: messages.AnyApiMessage): void {
    this._onPostMessage(message);
  }

  public get messages (): rxjs.Observable<messages.AnyApiMessage> {
    return this._messages;
  }
}
