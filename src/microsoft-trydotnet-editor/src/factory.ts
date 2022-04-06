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
  const wasmRunnerHostingWindow = wasmIframe.contentWindow;
  const wasmIframeMessages = new rxjs.Subject<IWasmRunnerMessage>();

  window.addEventListener('message', (message) => {
    const messageType = message.data.type as string;
    if (messageType && messageType.startsWith("wasmRunner-")) {
      //console.log(`[received from WASM runner] ${JSON.stringify(message)}`);
      const wasmRunnerMessage = message.data;
      if (wasmRunnerMessage) {
        wasmIframeMessages.next(wasmRunnerMessage);
      }
    }
  }, false);

  const postAndLogToWasmRunner = (message: any) => {
    // console.log(`[to WASM runner] ${JSON.stringify(message)}`);
    const targetWindow = wasmRunnerHostingWindow;
    const messageLogger = targetWindow['postMessageLogger'] || targetWindow.parent['postMessageLogger'] || window['postMessageLogger'];
    if (typeof (messageLogger) === 'function') {
      messageLogger(message);
    }

    targetWindow.postMessage(message, '*');
  };

  wasmIframe.src = configuration.wasmRunnerUrl;

  const wasmRunner = new WasmRunner(message => postAndLogToWasmRunner(message), wasmIframeMessages);
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

  constructor(
    private _postToWasmRunner: (message: any) => void,
    private _wasmIframeMessages: rxjs.Subject<IWasmRunnerMessage>) {
    if (!this._wasmIframeMessages) {
      throw new Error("wasmIframeMessages is required");
    }

  }

  public run(runRequest: {
    assembly: dotnetInteractive.Base64EncodedAssembly,
    onOutput: (output: string) => void,
    onError: (error: string) => void,
  }): Promise<void> {
    //console.log("WasmRunner.run starting");
    let completionSource = new dotnetInteractive.PromiseCompletionSource<IWasmRunnerMessage>();

    let sub = this._wasmIframeMessages.subscribe({
      next: (wasmRunnerMessage) => {
        //console.log(`WasmRunner message ${JSON.stringify(wasmRunnerMessage)}`);
        let type = wasmRunnerMessage.type;
        if (type) {
          switch (type) {
            case "wasmRunner-result":
              //console.log("WasmRunner execution completed");
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
      }
    });

    this._postToWasmRunner({
      type: "wasmRunner-command",
      base64EncodedAssembly: runRequest.assembly.value
    });

    return completionSource.promise.then(r => {
      sub.unsubscribe();
      //console.log("WasmRunner.run completed");
    });

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