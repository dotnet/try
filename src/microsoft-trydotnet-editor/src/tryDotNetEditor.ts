// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as messages from './legacyTryDotNetMessages';
import * as rxjs from 'rxjs';
import * as projectKernel from './projectKernel';
import * as monaco from './EditorAdapter';
import * as dotnetInteractive from '@microsoft/dotnet-interactive';
import * as newContract from './newContract';
import { DocumentId } from './documentId';
import { configureLogging } from './log';
export class TryDotNetEditor {
  private _editor?: monaco.EditorAdapter;
  private _currentProject?: { projectItems: dotnetInteractive.ProjectItem[]; };
  editorId: string;
  private _editorChangesSubscription: rxjs.Subscription;
  private _currentDocumentId?: DocumentId;
  private _currentDocument: dotnetInteractive.DocumentOpened;
  private _handlingRunRequest: boolean;


  public get isHandlingRunRequest(): boolean {
    return this._handlingRunRequest;
  }
  public get currentProject(): { projectItems: dotnetInteractive.ProjectItem[] } {
    return this._currentProject;
  }

  constructor(private _postMessage: (message: any) => void, private _mainWindowMessageBus: rxjs.Subject<any>, private _kernel: projectKernel.ProjectKernel) {
    this.editorId = "-0-";

    this._mainWindowMessageBus.subscribe(message => {
      this.handleHostMessage(message);
    });
    // for messaging api backward compatibility

    this._kernel.subscribeToKernelEvents((event) => {
      dotnetInteractive.Logger.default.info(`[kernel event] : ${JSON.stringify(event)}`);
      if (event.command.commandType === dotnetInteractive.SubmitCodeType) {
        dotnetInteractive.Logger.default.info(`[SubmitCode event] : ${JSON.stringify(event)}`);
        switch (event.eventType) {
          case dotnetInteractive.CommandSucceededType:
          case dotnetInteractive.CommandFailedType:
          case dotnetInteractive.CommandCancelledType:
            if (event.command.commandType === dotnetInteractive.SubmitCodeType) {
              this._postKernelEvent({ event });
              this._postMessage({
                type: messages.HOST_RUN_READY_EVENT
              });
            }
            break;
          case dotnetInteractive.CodeSubmissionReceivedType:
            this._postKernelEvent({ event });
            this._postMessage({
              type: messages.NOTIFY_HOST_RUN_BUSY
            });
            break;
          case dotnetInteractive.StandardOutputValueProducedType:
          case dotnetInteractive.StandardErrorValueProducedType:
          case dotnetInteractive.DisplayedValueProducedType:
            dotnetInteractive.Logger.default.info(`[kernel event] : ${JSON.stringify(event)}`);
            this._postKernelEvent({ event });
            break;
        }
      }
    });
  }

  private _postKernelEvent(arg: { event: dotnetInteractive.KernelEventEnvelope, editorId?: string, requestId?: string }) {
    let message = {
      type: arg.event.eventType,
      ...(arg.event)
    };
    if (arg.editorId) {
      (<any>message).editorId = arg.editorId;
    }

    if (arg.requestId) {
      (<any>message).requestId = arg.requestId;
    }
    this._postMessage(message);
  }

  public get editor(): monaco.EditorAdapter {
    return this._editor;
  }

  public set editor(value: monaco.EditorAdapter) {
    this._editor = value;
    this._editorChangesSubscription?.unsubscribe();
    if (this._editor) {
      dotnetInteractive.Logger.default.info('configuring editor');
      if (this._kernel) {
        this._editor.configureServices(this._kernel);
        // todo : this should be coming from the kernelInfo
        this._editor.setLanguage("csharp");
      }
      this._editorChangesSubscription = this._editor.editorChanges.subscribe({
        next: (change) => {
          dotnetInteractive.Logger.default.info(`[editor changes] : ${JSON.stringify(change)}`);

          const editorContentChanged: newContract.EditorContentChanged = {
            type: newContract.EditorContentChangedType,
            content: change.code,
            relativeFilePath: this._currentDocumentId.relativeFilePath,
            regionName: this._currentDocumentId.regionName,
            editorId: this.editorId
          };
          this._postMessage(editorContentChanged);
        }
      });
    }
  }

  public async handleHostMessage(apiMessage: {
    type: string;
    [index: string]: any
  }) {
    const requestId = apiMessage?.requestId;
    const editorId = apiMessage?.editorId;
    dotnetInteractive.Logger.default.info(`[tryDotNetEditor.handleHostMessage] : ${JSON.stringify(apiMessage)}`);
    switch (apiMessage.type) {
      case newContract.EnableLoggingType:
        configureLogging({ enableLogging: apiMessage.enableLogging });
        break;
      case newContract.ConfigureMonacoEditorType:
        let options = {};
        if (apiMessage.editorOptions) {
          options = { ...options, ...apiMessage.editorOptions };
        }
        if (apiMessage.theme) {
          options = { ...options, theme: apiMessage.theme };
        }
        this.getEditor().updateOptions(options);

        break;
      case newContract.SetEditorContentType:
        const content = (<newContract.SetEditorContent>apiMessage).content;
        this.getEditor().setCode(content);
        break;
      case newContract.DefineMonacoEditorThemesType:
        this.getEditor().defineTheme(apiMessage.themes);
        break;
      case dotnetInteractive.OpenProjectType:
        {
          await this.openProject(<dotnetInteractive.Project><any>(apiMessage.project));
          const message: newContract.ProjectOpened = {
            type: dotnetInteractive.ProjectOpenedType,
            projectItems: this._currentProject?.projectItems,
            requestId,
            editorId
          };
          this._postMessage(message);
        }
        break;
      case dotnetInteractive.OpenDocumentType:
        {
          const request = <newContract.OpenDocument>apiMessage;

          const documentId = new DocumentId(request);

          let documentOpened = await this.openDocument(documentId);

          const message: newContract.DocumentOpened = {
            type: dotnetInteractive.DocumentOpenedType,
            relativeFilePath: documentOpened.relativeFilePath,
            content: documentOpened.content,
            requestId,
            editorId
          };

          if (documentOpened.regionName) {
            message.regionName = documentOpened.regionName;
          }
          this._postMessage(message);

        }
        break;
      case messages.FOCUS_EDITOR_REQUEST:
        this.getEditor().focus();
        break;
      case messages.RUN_REQUEST:
        await this._handleRunRequest(requestId);
        break;
      default:
        dotnetInteractive.Logger.default.warn(`unhandled message: ${JSON.stringify(apiMessage)}`);
        break;
    }
  }

  private _handleRunRequest(requestId: string): Promise<void> {
    this._handlingRunRequest = true;
    const code = this.getEditor().getCode();
    const command: dotnetInteractive.KernelCommandEnvelope = {
      commandType: dotnetInteractive.SubmitCodeType,
      command: <dotnetInteractive.SubmitCode>{
        code: code
      }
    };

    dotnetInteractive.Logger.default.info(`[tryDotNetEditor.handleRunRequest] start`);

    let events: dotnetInteractive.KernelEventEnvelope[] = [];
    let sub = this.getKernel().subscribeToKernelEvents(event => {
      dotnetInteractive.Logger.default.info(`[tryDotNetEditor.handleRunRequest] kernel event: ${JSON.stringify(event)}`);
      switch (event.eventType) {
        case dotnetInteractive.CommandSucceededType:
        case dotnetInteractive.CommandFailedType:
        case dotnetInteractive.CommandCancelledType:
          if (event.command.commandType === dotnetInteractive.SubmitCodeType) {
            dotnetInteractive.Logger.default.info(`[tryDotNetEditor.handleRunRequest] completed : ${JSON.stringify(command)}`);

            dotnetInteractive.Logger.default.info(`[tryDotNetEditor.handleRunRequest]  disposing event subscription}`);
            sub.dispose();
            this._handlingRunRequest = false;

            let response: any = {
              type: messages.RUN_COMPLETED_EVENT,
              requestId: requestId,
              outcome: event.eventType === dotnetInteractive.CommandSucceededType ? 'Success' : 'Failed'
            };

            try {
              if (event.eventType === dotnetInteractive.CommandFailedType) {
                response.exception = (<dotnetInteractive.CommandFailed>event.event).message;
              }
              const stdOutEvents = events.filter(e => e.eventType === dotnetInteractive.StandardOutputValueProducedType).map(e => (<dotnetInteractive.StandardOutputValueProduced>e.event));

              response.output = stdOutEvents.map(e => e.formattedValues[0].value);

              const diagnosticsEvents = events.filter(e => e.eventType === dotnetInteractive.DiagnosticsProducedType).map(e => (<dotnetInteractive.DiagnosticsProduced>e.event));

              response.diagnostics = [];
              for (let diagnosticsEvent of diagnosticsEvents) {
                for (let diagnostic of diagnosticsEvent.diagnostics) {
                  response.diagnostics.push({
                    start: diagnostic.linePositionSpan.start,
                    end: diagnostic.linePositionSpan.end,
                    severity: diagnostic.severity,
                    message: diagnostic.message,
                  });
                }
              }
            }
            finally {
              this._postMessage(response);
              this._postMessage({
                type: messages.HOST_RUN_READY_EVENT,
                requestId: requestId,
              });
            }
          }
          break;
        default:
          if (event.command.commandType === dotnetInteractive.SubmitCodeType) {
            events.push(event);
          }
          break;
      }
    });

    dotnetInteractive.Logger.default.info(`[tryDotNetEditor.handleRunRequest] sending : ${JSON.stringify(command)}`);

    return this.getKernel()
      .send(command);
  }

  public async openProject(project: dotnetInteractive.Project) {
    const command: dotnetInteractive.KernelCommandEnvelope = {
      commandType: dotnetInteractive.OpenProjectType,
      command: <dotnetInteractive.OpenProject>{
        project: project
      }
    };//?
    this.getEditor().disableLanguageService();
    this.getEditor().disableTextChangedEvents();

    this._currentDocumentId = null;
    let projectOpened = await dotnetInteractive.submitCommandAndGetResult<dotnetInteractive.ProjectOpened>(
      this.getKernel(), command, dotnetInteractive.ProjectOpenedType); //?

    this.getEditor().enableTextChangedEvents();
    this.getEditor().enableLanguageService();
    this._currentProject = { ...projectOpened };
    dotnetInteractive.Logger.default.info(`[tryDotNetEditor.openProject] : ${JSON.stringify(projectOpened)}`);
    return projectOpened.projectItems;//?

  }

  public async openDocument(document: { relativeFilePath: string, regionName?: string }) {
    const documentId = new DocumentId(document);
    const command: dotnetInteractive.KernelCommandEnvelope = {
      commandType: dotnetInteractive.OpenDocumentType,
      command: <dotnetInteractive.OpenDocument>{
        relativeFilePath: document.relativeFilePath,
        regionName: document.regionName
      }
    };//?

    const shouldChaneEditor = !DocumentId.areEqual(documentId, this._currentDocumentId);
    if (shouldChaneEditor) {
      this._currentDocumentId = documentId;

      this.getEditor().disableLanguageService();
      this.getEditor().disableTextChangedEvents();
      this._currentDocument = await dotnetInteractive.submitCommandAndGetResult<dotnetInteractive.DocumentOpened>(
        this.getKernel(),
        command,
        dotnetInteractive.DocumentOpenedType); //?
      this.getEditor().setCode(this._currentDocument.content);
      this.getEditor().enableTextChangedEvents();
      this.getEditor().enableLanguageService();
    } else {
      this._currentDocument.content = this.getEditor().getCode();
    }

    dotnetInteractive.Logger.default.info(`[tryDotNetEditor.openDocument] : ${JSON.stringify(this._currentDocument)}`);
    return this._currentDocument;//?
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
}
