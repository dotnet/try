// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as rxjs from 'rxjs';
import * as messages from './messages';

export interface IMessageBus {
  postMessage(message: any): void;
  messages: rxjs.Observable<messages.AnyApiMessage>;
}

export class MessageBus implements IMessageBus {
  constructor(private _onPostMessage: (message: any) => void, private _messages: rxjs.Observable<messages.AnyApiMessage>) { }

  public postMessage(message: any): void {
    this._onPostMessage(message);
  }

  public get messages(): rxjs.Observable<any> {
    return this._messages;
  }
}
