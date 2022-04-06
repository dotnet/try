// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as messages from './messages';
import * as contract from './contract';
import * as rxjs from 'rxjs';
import * as projectKernel from './projectKernel';
import * as monaco from './EditorAdapter';
import * as dotnetInteractive from '@microsoft/dotnet-interactive';

export class TryDotNetEditor {
  private _editor?: monaco.EditorAdapter;

  constructor(private _postMessage: (message: any) => void, private _mainWindowMessageBus: rxjs.Subject<any>, private _kernel: projectKernel.ProjectKernel) {
    this._mainWindowMessageBus.subscribe(message => {
      this.onHostMessage(message);
    });
    // for messaging api backward compatibility
    this._kernel.subscribeToKernelEvents((event) => {
      // console.log(`[kernel events event] : ${JSON.stringify(event)}`);
      if (event.command.commandType === dotnetInteractive.SubmitCodeType) {
        // console.log(`[SubmitCode event] : ${JSON.stringify(event)}`);
        switch (event.eventType) {
          case dotnetInteractive.CommandSucceededType:
          case dotnetInteractive.CommandFailedType:
          case dotnetInteractive.CommandCancelledType:
            this._postMessage({
              type: "NOTIFY_HOST_RUN_COMPLETED"
            });
            this._postMessage({
              type: "NOTIFY_HOST_RUN_READY"
            });
            break;
          case dotnetInteractive.CodeSubmissionReceivedType:
            this._postMessage({
              type: "NOTIFY_HOST_RUN_BUSY"
            });
            break;
          case dotnetInteractive.StandardOutputValueProducedType:
            this._postMessage({
              type: dotnetInteractive.StandardOutputValueProducedType,
              event: event
            });
            break;
          case dotnetInteractive.StandardErrorValueProducedType:
            this._postMessage({
              type: dotnetInteractive.StandardErrorValueProducedType,
              event: event
            });
            break;
        }
      } else {
        switch (event.eventType) {
          case dotnetInteractive.ProjectOpenedType:
            this._postMessage({
              type: "PROJECT_LOADED",
              event: event
            });
            break;
          case dotnetInteractive.DocumentOpenedType:
            this._postMessage({
              type: "DOCUMENT_OPENED",
              event: event
            });
            break;
        }

      }
    });
  }

  public get editor(): monaco.EditorAdapter {
    return this._editor;
  }

  public set editor(value: monaco.EditorAdapter) {
    this._editor = value;
    if (this._editor) {
      //console.log('configuring editor');
      if (this._kernel) {
        this._editor.configureServices(this._kernel);
        // todo : this should be coming from the kernelInfo
        this._editor.setLanguage("csharp");
      }
    }
  }

  private onHostMessage(apiMessage: messages.AnyApiMessage) {
    switch (apiMessage.type) {
      case messages.CONFIGURE_MONACO_REQUEST:
        let options = {};
        if (apiMessage.editorOptions) {
          options = { ...options, ...apiMessage.editorOptions };
        }
        if (apiMessage.theme) {
          options = { ...options, theme: apiMessage.theme };
        }
        this.getEditor().updateOptions(options);

        break;
      case messages.SET_EDITOR_CODE_REQUEST:
        const requestId = apiMessage.requestId;
        const sourceCode = apiMessage.sourceCode;
        this.updateWorkspaceBuffer(sourceCode);
        break;
      case messages.DEFINE_THEMES_REQUEST:
        const source = { ...apiMessage };
        this.getEditor().defineTheme(apiMessage.themes);
        break;
      case messages.SET_WORKSPACE_REQUEST:
        const project =
          this.configureWorkspace(apiMessage.workspace);
        break;
      case messages.SET_ACTIVE_BUFFER_REQUEST:
        const bufferId = apiMessage.bufferId;
        const parts = bufferId.split('@');
        const command: dotnetInteractive.OpenDocument = {
          relativeFilePath: parts[0],
        };
        if (parts.length > 1) {
          command.regionName = parts[1];
        }
        this.getKernel().send({
          commandType: dotnetInteractive.OpenDocumentType,
          command: command
        });

        break;
      case messages.RUN_REQUEST: {
        const code = this.getEditor().getCode();
        this.getKernel().send({
          commandType: dotnetInteractive.SubmitCodeType,
          command: <dotnetInteractive.SubmitCode>{
            code: code
          }
        });

      }
    }
  }

  private async configureWorkspace(workspace: contract.IWorkspace) {

    function toProject(workspace: contract.IWorkspace): dotnetInteractive.Project {
      let project: dotnetInteractive.Project = {
        files: workspace.buffers.map(buffer => <dotnetInteractive.ProjectFile>{ relativeFilePath: buffer.id, content: buffer.content }),
      };

      return project;
    }

    const project = toProject(workspace);
    await this.openProject(project);
    if (workspace.activeBufferId) {
      const bufferId = workspace.activeBufferId;
      const parts = bufferId.split('@');
      const command: dotnetInteractive.OpenDocument = {
        relativeFilePath: parts[0],
      };
      if (parts.length > 1) {
        command.regionName = parts[1];
      }
      await this.getKernel().send({
        commandType: dotnetInteractive.OpenDocumentType,
        command: command
      });

    }
  }

  public async openProject(project: dotnetInteractive.Project) {
    await this.getKernel().send({
      commandType: dotnetInteractive.OpenProjectType,
      command: <dotnetInteractive.OpenProject>{
        project: project
      }
    });
  }

  public async openDocument(document: { relativeFilePath: string, regionName?: string }) {
    await this.getKernel().send({
      commandType: dotnetInteractive.OpenDocumentType,
      command: <dotnetInteractive.OpenDocument>{
        relativeFilePath: document.relativeFilePath,
        regionName: document.regionName,
      }
    });
  }

  private getEditor(): monaco.EditorAdapter {
    if (this._editor) {
      return this._editor;
    } else {
      throw new Error('Editor not initialized');
    }
  }

  private getKernel(): projectKernel.ProjectKernel {
    if (this._kernel) {
      return this._kernel;
    } else {
      throw new Error('Kernel not initialized');
    }
  }

  private updateWorkspaceBuffer(sourceCode: string) {
    this.getEditor().setCode(sourceCode);
  }
}
