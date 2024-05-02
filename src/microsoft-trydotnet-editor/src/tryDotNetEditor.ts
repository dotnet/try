// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as messages from './legacyTryDotNetMessages';
import * as rxjs from 'rxjs';
import * as projectKernel from './projectKernel';
import * as monaco from './EditorAdapter';
import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';
import * as newContract from './newContract';
import { DocumentId } from './documentId';
import { configureLogging } from './log';
import { DebouncingKernel } from './debouncingKernel';

export class TryDotNetEditor {
  private _editor?: monaco.EditorAdapter;
  private _currentProject?: { projectItems: polyglotNotebooks.ProjectItem[]; };
  editorId: string;
  private _editorChangesSubscription: rxjs.Subscription;
  private _currentDocumentId?: DocumentId;
  private _currentDocument: polyglotNotebooks.DocumentOpened;
  private _handlingRunRequest: boolean;
  private _kernel: DebouncingKernel;


  public get isHandlingRunRequest(): boolean {
    return this._handlingRunRequest;
  }
  public get currentProject(): { projectItems: polyglotNotebooks.ProjectItem[] } {
    return this._currentProject;
  }

  constructor(private _postMessage: (message: any) => void, private _mainWindowMessageBus: rxjs.Subject<any>, kernel: projectKernel.ProjectKernel) {
    this.editorId = "-0-";

    this._kernel = new DebouncingKernel(kernel);
    this._mainWindowMessageBus.subscribe(message => {
      this.handleHostMessage(message);
    });
    // for messaging api backward compatibility

    this._kernel.subscribeToKernelEvents((event) => {
      polyglotNotebooks.Logger.default.info(`[kernel event] : ${JSON.stringify(event)}`);
      if (event.command.commandType === polyglotNotebooks.SubmitCodeType) {
        polyglotNotebooks.Logger.default.info(`[SubmitCode event] : ${JSON.stringify(event)}`);
        switch (event.eventType) {
          case polyglotNotebooks.CommandSucceededType:
          case polyglotNotebooks.CommandFailedType:
            if (event.command.commandType === polyglotNotebooks.SubmitCodeType) {
              this._postKernelEvent({ event });
              this._postMessage({
                type: messages.HOST_RUN_READY_EVENT
              });
            }
            break;
          case polyglotNotebooks.CodeSubmissionReceivedType:
            this._postKernelEvent({ event });
            this._postMessage({
              type: messages.NOTIFY_HOST_RUN_BUSY
            });
            break;
          case polyglotNotebooks.StandardOutputValueProducedType:
          case polyglotNotebooks.StandardErrorValueProducedType:
          case polyglotNotebooks.DisplayedValueProducedType:
            polyglotNotebooks.Logger.default.info(`[kernel event] : ${JSON.stringify(event)}`);
            this._postKernelEvent({ event });
            break;
        }
      }
    });
  }

  private _postKernelEvent(arg: { event: polyglotNotebooks.KernelEventEnvelope, editorId?: string, requestId?: string }) {
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
      polyglotNotebooks.Logger.default.info('configuring editor');
      if (this._kernel) {
        this._editor.configureServices(this._kernel);
        // todo : this should be coming from the kernelInfo
        this._editor.setLanguage("csharp");
      }
      this._editorChangesSubscription = this._editor.editorChanges.subscribe({
        next: (change) => {
          polyglotNotebooks.Logger.default.info(`[editor changes] : ${JSON.stringify(change)}`);

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
    polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.handleHostMessage] : ${JSON.stringify(apiMessage)}`);
    switch (apiMessage.type) {
      case newContract.EnableLoggingType:
        configureLogging({ enableLogging: apiMessage.enableLogging });
        break;
      case newContract.SetMonacoEditorSizeType:
        let size = (<newContract.SetMonacoEditorSize>apiMessage).size;
        this.getEditor().layout(size);
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
      case polyglotNotebooks.OpenProjectType:
        {
          await this.openProject(<polyglotNotebooks.Project><any>(apiMessage.project));
          const message: newContract.ProjectOpened = {
            type: polyglotNotebooks.ProjectOpenedType,
            projectItems: this._currentProject?.projectItems,
            requestId,
            editorId
          };
          this._postMessage(message);
        }
        break;
      case polyglotNotebooks.OpenDocumentType:
        {
          const request = <newContract.OpenDocument>apiMessage;

          const documentId = new DocumentId(request);

          let documentOpened = await this.openDocument(documentId);

          const message: newContract.DocumentOpened = {
            type: polyglotNotebooks.DocumentOpenedType,
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
        polyglotNotebooks.Logger.default.warn(`unhandled message: ${JSON.stringify(apiMessage)}`);
        break;
    }
  }

  public run(): Promise<void> {
    const code = this.getEditor().getCode();

    const command = polyglotNotebooks.KernelCommandEnvelope.fromJson({
      commandType: polyglotNotebooks.SubmitCodeType,
      command: <polyglotNotebooks.SubmitCode>{
        code: code
      }
    });

    polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.run] sending : ${JSON.stringify(command)}`);

    return this.getKernel()
      .send(command);
  }

  public subscribeToKernelEvents(observer: polyglotNotebooks.KernelEventEnvelopeObserver): polyglotNotebooks.DisposableSubscription {
    return this.getKernel().subscribeToKernelEvents(observer);
  }

  private _handleRunRequest(requestId: string): Promise<void> {
    this._handlingRunRequest = true;

    polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.handleRunRequest] start`);

    let events: polyglotNotebooks.KernelEventEnvelope[] = [];
    let sub = this.subscribeToKernelEvents(event => {
      polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.handleRunRequest] kernel event: ${JSON.stringify(event)}`);
      switch (event.eventType) {
        case polyglotNotebooks.CommandSucceededType:
        case polyglotNotebooks.CommandFailedType:
          if (event.command.commandType === polyglotNotebooks.SubmitCodeType) {
            polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.handleRunRequest] completed : ${JSON.stringify(event.command)}`);

            polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.handleRunRequest]  disposing event subscription}`);
            sub.dispose();
            this._handlingRunRequest = false;

            let response: any = {
              type: messages.RUN_COMPLETED_EVENT,
              requestId: requestId,
              outcome: event.eventType === polyglotNotebooks.CommandSucceededType ? 'Success' : 'Failed'
            };

            try {
              response.exception = [];

              const stdOutEvents = events
                .filter(e => e.eventType === polyglotNotebooks.StandardOutputValueProducedType)
                .map(e => (<polyglotNotebooks.StandardOutputValueProduced>e.event));

              const diagnosticsEvents = events
                .filter(e => e.eventType === polyglotNotebooks.DiagnosticsProducedType)
                .map(e => (<polyglotNotebooks.DiagnosticsProduced>e.event));

              response.output = [stdOutEvents.map(e => e.formattedValues[0].value).join('')];

              response.diagnostics = [];
              for (let diagnosticsEvent of diagnosticsEvents) {
                for (let diagnostic of diagnosticsEvent.diagnostics) {
                  if (diagnostic.severity !== polyglotNotebooks.DiagnosticSeverity.Hidden) {
                    response.exception.push(diagnostic.message);
                  }
                  response.diagnostics.push({
                    start: diagnostic.linePositionSpan.start,
                    end: diagnostic.linePositionSpan.end,
                    severity: diagnostic.severity,
                    message: diagnostic.message,
                  });
                }
              }

              if (response.exception.length === 0) {
                response.exception = null;
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
          events.push(event);
          break;
      }
    });

    return this.run();
  }

  public async openProject(project: polyglotNotebooks.Project) {
    const command = polyglotNotebooks.KernelCommandEnvelope.fromJson({
      commandType: polyglotNotebooks.OpenProjectType,
      command: <polyglotNotebooks.OpenProject>{
        project: project
      }
    });//?
    this.getEditor().disableLanguageService();
    this.getEditor().disableTextChangedEvents();

    this._currentDocumentId = null;
    let projectOpened = await polyglotNotebooks.submitCommandAndGetResult<polyglotNotebooks.ProjectOpened>(
      this.getKernel().asInteractiveKernel(), command, polyglotNotebooks.ProjectOpenedType); //?

    this.getEditor().enableTextChangedEvents();
    this.getEditor().enableLanguageService();
    this._currentProject = { ...projectOpened };
    polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.openProject] : ${JSON.stringify(projectOpened)}`);
    return projectOpened.projectItems;//?

  }

  public async openDocument(document: { relativeFilePath: string, regionName?: string }) {
    const documentId = new DocumentId(document);
    const command = polyglotNotebooks.KernelCommandEnvelope.fromJson({
      commandType: polyglotNotebooks.OpenDocumentType,
      command: <polyglotNotebooks.OpenDocument>{
        relativeFilePath: document.relativeFilePath,
        regionName: document.regionName
      }
    });//?

    const shouldChaneEditor = !DocumentId.areEqual(documentId, this._currentDocumentId);
    if (shouldChaneEditor) {
      this._currentDocumentId = documentId;

      this.getEditor().disableLanguageService();
      this.getEditor().disableTextChangedEvents();
      this._currentDocument = await polyglotNotebooks.submitCommandAndGetResult<polyglotNotebooks.DocumentOpened>(
        this.getKernel().asInteractiveKernel(),
        command,
        polyglotNotebooks.DocumentOpenedType); //?
      this.getEditor().setCode(this._currentDocument.content);
      this.getEditor().enableTextChangedEvents();
      this.getEditor().enableLanguageService();
    } else {
      this._currentDocument.content = this.getEditor().getCode();
    }

    polyglotNotebooks.Logger.default.info(`[tryDotNetEditor.openDocument] : ${JSON.stringify(this._currentDocument)}`);
    return this._currentDocument;//?
  }

  private getEditor(): monaco.EditorAdapter {
    if (this._editor) {
      return this._editor;
    } else {
      throw new Error('Editor not initialized');
    }
  }

  private getKernel(): DebouncingKernel {
    if (this._kernel) {
      return this._kernel;
    } else {
      throw new Error('Kernel not initialized');
    }
  }
}
