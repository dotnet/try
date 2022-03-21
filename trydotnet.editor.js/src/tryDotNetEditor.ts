// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as messages from './messages';
import * as contract from './contract';

import * as apiService from './apiService';
import * as messageBus from './messageBus';
import * as projectKernel from './projectKernel';
import * as monaco from './EditorAdapter';
import * as dotnetInteractive from '@microsoft/dotnet-interactive';

export class TryDotNetEditor {
  private _editor?: monaco.EditorAdapter;

  constructor(private _mainWindowMessageBus: messageBus.IMessageBus, private _kernel: projectKernel.ProjectKernel) {
    this._mainWindowMessageBus.messages.subscribe(message => {
      this.onHostMessage(message);
    });
  }

  public get editor(): monaco.EditorAdapter {
    return this._editor;
  }

  public set editor(value: monaco.EditorAdapter) {
    this._editor = value;
    if (this._editor) {
      console.log('configuring editor');
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
    }
  }

  private async configureWorkspace(workspace: contract.IWorkspace) {

    function toProject(workspace: contract.IWorkspace): dotnetInteractive.Project {
      let project: dotnetInteractive.Project = {
        files: workspace.buffers.map(buffer => <dotnetInteractive.ProjectFile>{ relativePath: buffer.id, content: buffer.content }),
      };

      return project;
    }

    const project = toProject(workspace);
    await this.openProject(project);
  }

  public async openProject(project: dotnetInteractive.Project) {
    await this.getKernel().send({
      commandType: dotnetInteractive.OpenProjectType,
      command: <dotnetInteractive.OpenProject>{
        project: project
      }
    });
  }

  public async openDocument(document: { path: string, regionName?: string }) {
    await this.getKernel().send({
      commandType: dotnetInteractive.OpenDocumentType,
      command: <dotnetInteractive.OpenDocument>{
        path: document.path,
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
