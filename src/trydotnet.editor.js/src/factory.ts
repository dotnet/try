// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monaco from 'monaco-editor';
import * as rxjs from 'rxjs';
import { ProjectKernelWithWASMRunner } from './ProjectKernelWithWASMRunner';
import * as messageBus from './messageBus';
import * as messages from './messages';

import { ProjectKernel } from "./projectKernel";
import { IWasmRunner } from './wasmRunner';
import { createApiService } from './apiService';
import * as dotnetInteractive from '@microsoft/dotnet-interactive';

export function createWasmProjectKernel(): ProjectKernel {
  const wasmIframe = document.createElement('iframe');
  // hide the frame from screen readres.
  wasmIframe.setAttribute('aria-hidden', 'true');
  wasmIframe.setAttribute('height', '0px');
  wasmIframe.setAttribute('width', '0px');
  wasmIframe.setAttribute('role', 'wasm-runner');

  document.body.appendChild(wasmIframe);
  const hostWindow = wasmIframe.contentWindow;
  const wasmIframeMessages = new rxjs.Subject<messages.AnyApiMessage>();

  hostWindow.addEventListener('message', (event) => {
    const apiMessage = <messages.AnyApiMessage>event.data;
    if (apiMessage) {
      wasmIframeMessages.next(apiMessage);
    }
  });

  const wasmIframeBus = new messageBus.MessageBus((message: messages.AnyApiMessage) => {
    hostWindow.postMessage(message, '*');
  },
    wasmIframeMessages
  );

  const wasmRunner = new WasmRunner(wasmIframeBus);
  let runner: IWasmRunner = (runRequest) => {
    return wasmRunner.run(runRequest);
  };

  const apiService = createApiService();

  // todo  : this should be not a problem in .NET Interactive library
  dotnetInteractive.Logger.configure("debug", (_entry) => {

  });

  return new ProjectKernelWithWASMRunner("csharp", runner, apiService);
}

export function createEditor(container: HTMLElement) {
  const editor = monaco.editor.create(container, {
    value: 'console.log("Hello, world")',
    language: 'csharp'
  });
  return editor;
}


export class WasmRunner {

  constructor(private _messageBus: messageBus.IMessageBus,) {
    if (!this._messageBus) {
      throw new Error("messageBus is required");
    }

  }

  public run(runRequest: {
    assembly?: dotnetInteractive.Base64EncodedAssembly,
    onOutput: (output: string) => void,
    onError: (error: string) => void,
  }): Promise<void> {
    throw new Error('Method not implemented.');
  }
}
