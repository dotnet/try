// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as messages from './legacyTryDotNetMessages';
import * as legacyContract from './legacyContract';
import * as rxjs from 'rxjs';
import * as projectKernel from './projectKernel';
import * as monaco from './EditorAdapter';
import * as dotnetInteractive from '@microsoft/dotnet-interactive';
import * as newContract from './newContract';

function areSameFile(fileOne: string, fileTwo: string): boolean {
  return fileOne.replace(/\.\//g, "") === fileTwo.replace(/\.\//g, "");
}
function isNullOrUndefinedOrWhitespace(input: string): boolean {
  if (isNullOrUndefined(input)) {
    return true;
  }
  return input.replace(/\s/g, "").length < 1;
}

function isNullOrUndefined(input: string): boolean {
  return input === undefined || input === null;
}
class DocumentId {
  private _relativeFilePath: string;
  private _regionName: string;
  private _stringValue: string;
  toString(): string {
    return this._stringValue;
  }

  constructor(documentId: { relativeFilePath: string, regionName?: string }) {
    this._relativeFilePath = documentId.relativeFilePath;
    this._regionName = documentId.regionName;
    this._stringValue = this._relativeFilePath;
    if (!isNullOrUndefinedOrWhitespace(this._regionName)) {
      this._stringValue = `${this._relativeFilePath}@${this._regionName}`;
    }
  }

  public get relativeFilePath(): string {
    return this._relativeFilePath;
  }

  public get regionName(): string | undefined {
    return this._regionName;
  }

  public static areEqual(a: DocumentId, b: DocumentId): boolean {
    let ret = a === b;
    if (!ret) {
      if (a !== undefined && b !== undefined) {
        ret = a.equal(b);
      }

    }
    return ret;
  }

  public equal(other: DocumentId): boolean {
    return areSameFile(this.relativeFilePath, other.relativeFilePath) && this.regionName === other.regionName;
  }


  public static parse(documentId: string): DocumentId {
    const parts = documentId.split("@");//?
    return parts.length === 1
      ? new DocumentId({ relativeFilePath: parts[0], regionName: parts[1] })
      : new DocumentId({ relativeFilePath: parts[0] });

  }
}

export class TryDotNetEditor {
  private _editor?: monaco.EditorAdapter;
  private _currentProject?: { projectItems: dotnetInteractive.ProjectItem[]; };
  editorId: string;
  private _editorChangesSubscription: rxjs.Subscription;
  private _currentDocumentId?: DocumentId;
  private _currentDocument: dotnetInteractive.DocumentOpened;


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
      dotnetInteractive.Logger.default.info(`[kernel events event] : ${JSON.stringify(event)}`);
      if (event.command.commandType === dotnetInteractive.SubmitCodeType) {
        dotnetInteractive.Logger.default.info(`[SubmitCode event] : ${JSON.stringify(event)}`);
        switch (event.eventType) {
          case dotnetInteractive.CommandSucceededType:
          case dotnetInteractive.CommandFailedType:
          case dotnetInteractive.CommandCancelledType:
            if (event.command.commandType === dotnetInteractive.SubmitCodeType) {

              this._postMessage({
                type: event.eventType,
                ...event
              });
              this._postMessage({
                type: messages.HOST_RUN_READY_EVENT
              });
            }
            break;
          case dotnetInteractive.CodeSubmissionReceivedType:
            this._postMessage({
              type: messages.NOTIFY_HOST_RUN_BUSY
            });
            break;
          case dotnetInteractive.StandardOutputValueProducedType:
            this._postMessage({
              type: dotnetInteractive.StandardOutputValueProducedType,
              ...event
            });
            break;
          case dotnetInteractive.StandardErrorValueProducedType:
            this._postMessage({
              type: dotnetInteractive.StandardErrorValueProducedType,
              ...event
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

  public async handleHostMessage(apiMessage: messages.AnyApiMessage) {
    const requestId = apiMessage?.requestId;
    const editorId = apiMessage?.editorId;
    dotnetInteractive.Logger.default.info(`[tryDotNetEditor.handleHostMessage] : ${JSON.stringify(apiMessage)}`);
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
        const sourceCode = apiMessage.sourceCode;
        this.getEditor().setCode(sourceCode);
        break;
      case messages.DEFINE_THEMES_REQUEST:
        const source = { ...apiMessage };
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
      case messages.SET_WORKSPACE_REQUEST:
        {
          dotnetInteractive.Logger.default.info(`[tryDotNetEditor set workspace request] : ${JSON.stringify(apiMessage)}`);
          await this.configureWorkspace(apiMessage.workspace, requestId);
          const projectOpenedMessage: newContract.ProjectOpened = {
            type: dotnetInteractive.ProjectOpenedType,
            projectItems: this._currentProject?.projectItems,
            requestId,
            editorId
          };
          this._postMessage(projectOpenedMessage);
          dotnetInteractive.Logger.default.info(`[tryDotNetEditor set workspace request project opened] : ${JSON.stringify(projectOpenedMessage)}`);
          if (this._currentDocument) {

            const documentOpenMessage: newContract.DocumentOpened = {
              type: dotnetInteractive.DocumentOpenedType,
              content: this._currentDocument.content,
              relativeFilePath: this._currentDocument.relativeFilePath,
              regionName: this._currentDocument.regionName,
              requestId,
              editorId
            };

            this._postMessage(documentOpenMessage);
            dotnetInteractive.Logger.default.info(`[tryDotNetEditor set workspace request document opened] : ${JSON.stringify(documentOpenMessage)}`);
          }
          dotnetInteractive.Logger.default.info(`[tryDotNetEditor set workspace request done] : ${JSON.stringify(projectOpenedMessage)}`);
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
      case dotnetInteractive.OpenDocumentType:
        {

          let documentOpened = await this.openDocument(<newContract.OpenDocument>apiMessage);

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
      case messages.RUN_REQUEST: {
        const code = this.getEditor().getCode();
        const command: dotnetInteractive.KernelCommandEnvelope = {
          commandType: dotnetInteractive.SubmitCodeType,
          command: <dotnetInteractive.SubmitCode>{
            code: code
          }
        };

        dotnetInteractive.Logger.default.info(`[tryDotNetEditor run request] : ${JSON.stringify(command)}`);

        let events: dotnetInteractive.KernelEventEnvelope[] = [];
        let sub = this.getKernel().subscribeToKernelEvents(event => {
          switch (event.eventType) {
            case dotnetInteractive.CommandSucceededType:
            case dotnetInteractive.CommandFailedType:
            case dotnetInteractive.CommandCancelledType:
              if (event.command.commandType === dotnetInteractive.SubmitCodeType) {
                sub.dispose();
                let response: any = {
                  type: messages.RUN_COMPLETED_EVENT,
                  requestId: requestId,
                  outcome: event.eventType === dotnetInteractive.CommandSucceededType ? 'Success' : 'Failed'
                };

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
                this._postMessage(response);
                this._postMessage({
                  type: messages.HOST_RUN_READY_EVENT,
                  requestId: requestId,
                });
              }
              break;
            default:
              events.push(event);
              break;
          }
        });
        this.getKernel().send(command);

      }
      default:
        dotnetInteractive.Logger.default.warn(`unhandled message: ${JSON.stringify(apiMessage)}`);
        break;
    }
  }

  private async configureWorkspace(workspace: legacyContract.IWorkspace, requestId: string) {

    function toProject(ws: legacyContract.IWorkspace): dotnetInteractive.Project {
      let p: dotnetInteractive.Project = {
        files: []
      };

      if (ws.files) {
        for (let wsfile of ws.files) {
          let projectFile: dotnetInteractive.ProjectFile = {
            relativeFilePath: wsfile.name, content: wsfile.text
          };
          p.files.push(projectFile);
        }
        return p;
      }

      if (ws.buffers) {
        for (let wsbuffer of ws.buffers) {
          let projectFile: dotnetInteractive.ProjectFile = {
            relativeFilePath: wsbuffer.id, content: wsbuffer.content
          };
          p.files.push(projectFile);
        }
        return p;
      }
    }

    const project = toProject(workspace);
    dotnetInteractive.Logger.default.info(`[tryDotNetEditor configureWorkspace] : ${JSON.stringify(project)}`);
    await this.openProject(project);



    const bufferId = workspace.activeBufferId ?? workspace.files[0].name;
    const documentId = DocumentId.parse(bufferId);
    await this.openDocument(documentId);

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
        this.getKernel(), command, dotnetInteractive.DocumentOpenedType); //?
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
