// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monaco from 'monaco-editor';
import * as rxjs from 'rxjs';
import { ProjectKernelWithWASMRunner } from './ProjectKernelWithWASMRunner';

import { ProjectKernel } from "./projectKernel";
import { IWasmRunner } from './wasmRunner';
import { createApiService, IServiceError } from './apiService';
import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';

export function createWasmProjectKernel(onServiceError: (serviceError: IServiceError) => void): ProjectKernel {

  try {
    const wasmIframe = document.createElement('iframe');
    // hide the frame from screen readres.
    wasmIframe.setAttribute('aria-hidden', 'true');
    wasmIframe.setAttribute('height', '0px');
    wasmIframe.setAttribute('width', '0px');
    wasmIframe.setAttribute('role', 'wasm-runner');
    // remove the frame from the tab order.
    wasmIframe.setAttribute('tabindex', '-1');

    const configuration: IConfiguration = JSON.parse(document.getElementById("trydotnet-editor-script").dataset.trydotnetConfiguration);

    document.body.appendChild(wasmIframe);
    const wasmRunnerHostingWindow = wasmIframe.contentWindow;
    const wasmIframeMessages = new rxjs.Subject<IWasmRunnerMessage>();

    window.addEventListener('message', (message) => {
      const messageType = message.data.type as string;
      if (messageType && messageType.startsWith("wasmRunner-")) {
        polyglotNotebooks.Logger.default.info(`[received from WASM runner] ${JSON.stringify(message)}`);
        const wasmRunnerMessage = message.data;
        if (wasmRunnerMessage) {
          wasmIframeMessages.next(wasmRunnerMessage);
        }
      }
    }, false);

    const postAndLogToWasmRunner = (message: any) => {
      polyglotNotebooks.Logger.default.info(`[to WASM runner] ${JSON.stringify(message)}`);
      const targetWindow = wasmRunnerHostingWindow;
      const messageLogger = window['postMessageLogger'];
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
      referer: configuration.refererUrl ? new URL(configuration.refererUrl) : null,
      onServiceError: onServiceError
    });


    return new ProjectKernelWithWASMRunner("csharp", runner, apiService);
  } catch (e) {
    onServiceError({
      statusCode: "500",
      message: e.message
    });
  }
}

export function createEditor(container: HTMLElement) {

  const editor = monaco.editor.create(container, {
    value: '',
    language: 'csharp',
    scrollBeyondLastLine: false,
    selectOnLineNumbers: true,
    minimap: {
      enabled: false
    }
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
    assembly: polyglotNotebooks.Base64EncodedAssembly,
    onOutput: (output: string) => void,
    onError: (error: string) => void,
  }): Promise<void> {
    polyglotNotebooks.Logger.default.info("WasmRunner.run starting");
    let completionSource = new polyglotNotebooks.PromiseCompletionSource<IWasmRunnerMessage>();

    let sub = this._wasmIframeMessages.subscribe({
      next: (wasmRunnerMessage) => {
        polyglotNotebooks.Logger.default.info(`WasmRunner message ${JSON.stringify(wasmRunnerMessage)}`);
        let type = wasmRunnerMessage.type;
        if (type) {
          switch (type) {
            case "wasmRunner-result":
              polyglotNotebooks.Logger.default.info("WasmRunner execution completed");
              if (wasmRunnerMessage.result && !wasmRunnerMessage.result.success) {
                runRequest.onOutput(wasmRunnerMessage.result.error || wasmRunnerMessage.result.runnerError);
              }
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
      polyglotNotebooks.Logger.default.info("WasmRunner.run completed");
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

export interface IConfiguration {
  editorContainer?: string;
  wasmRunnerUrl: string,
  refererUrl: string,
  commandsUrl: string,
  enableLogging: boolean
} 