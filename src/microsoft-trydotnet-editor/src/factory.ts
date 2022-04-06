// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monaco from 'monaco-editor';
import * as rxjs from 'rxjs';
import { ProjectKernelWithWASMRunner } from './ProjectKernelWithWASMRunner';

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

  const configuration: IConfiguration = JSON.parse(document.getElementById("trydotnet-editor-script").dataset.trydotnetConfiguration);

  document.body.appendChild(wasmIframe);
  const hostWindow = wasmIframe.contentWindow;
  const wasmIframeMessages = new rxjs.Subject<IWasmRunnerMessage>();

  hostWindow.addEventListener('message', (event) => {
    const wasmRunnerMessage = event.data;
    if (wasmRunnerMessage) {
      wasmIframeMessages.next(wasmRunnerMessage);
    }
  });

  const postAndLog = (message: any) => {
    hostWindow.postMessage(message, '*');
    const messageLogger = hostWindow['postMessageLogger'] || window['postMessageLogger'];
    if (typeof (messageLogger) === 'function') {
      messageLogger(message);
    }
  };

  wasmIframe.src = configuration.wasmRunnerUrl;

  const wasmRunner = new WasmRunner(message => postAndLog(message), wasmIframeMessages);
  let runner: IWasmRunner = (runRequest) => {
    return wasmRunner.run(runRequest);
  };

  const apiService = createApiService({
    commandsUrl: new URL(configuration.commandsUrl),
    referer: configuration.refererUrl ? new URL(configuration.refererUrl) : null
  });


  return new ProjectKernelWithWASMRunner("csharp", runner, apiService);
}

export function createEditor(container: HTMLElement) {
  const editor = monaco.editor.create(container, {
    value: '',
    language: 'csharp'
  });
  return editor;
}


class WasmRunner {

  constructor(private _postMessage: (message: any) => void, private _wasmIframeMessages: rxjs.Subject<IWasmRunnerMessage>) {
    if (!this._wasmIframeMessages) {
      throw new Error("wasmIframeMessages is required");
    }

  }

  public async run(runRequest: {
    assembly: dotnetInteractive.Base64EncodedAssembly,
    onOutput: (output: string) => void,
    onError: (error: string) => void,
  }): Promise<void> {

    let completionSource = new dotnetInteractive.PromiseCompletionSource<IWasmRunnerMessage>();

    let sub = this._wasmIframeMessages.subscribe((wasmRunnerMessage) => {
      let type = wasmRunnerMessage.type;
      if (type) {
        switch (type) {
          case "wasmRunner-result":
            completionSource.resolve(wasmRunnerMessage);
            break;
          case "wasmRunner-stdout":
            runRequest.onOutput(wasmRunnerMessage.message);
            break;
          case "wasmRunner-stderror":
            runRequest.onError(wasmRunnerMessage.message);
            break;
        }
      }
    });
    this._postMessage({
      type: "wasmRunner-command",
      base64EncodedAssembly: runRequest.assembly.value
    });

    await completionSource.promise;
    sub.unsubscribe();
  }
}

interface IWasmRunnerMessage {
  type: string,
  result?: {
    success: boolean,
    error?: string,
    runnerError?: string
  },
  message?: string,
  success?: boolean
}

interface IConfiguration {
  wasmRunnerUrl: string,
  refererUrl: string,
  commandsUrl: string
} 