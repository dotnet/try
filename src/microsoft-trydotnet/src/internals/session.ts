// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import { Configuration } from "../configuration";
import { RequestIdGenerator, IRequestIdGenerator } from "./requestIdGenerator";
import { Workspace } from "./workspace";
import { Region, IDocument } from "../editableDocument";
import { executionService } from "./executionService";
import { SHOW_EDITOR_REQUEST, HOST_EDITOR_READY_EVENT, HOST_RUN_READY_EVENT, ApiMessage } from "../apiMessages";
import { MonacoTextEditor } from "./monacoTextEditor";
import { ITextEditor, TextChangedEvent } from "../editor";
import { Project } from "../project";
import { debounceTime, Subject, Unsubscribable } from "rxjs";
import { isNullOrUndefined } from "../stringExtensions";
import { ServiceError, ISession, OutputEventSubscriber, ServiceErrorSubscriber, OpenDocumentParameters, RunConfiguration, RunResult } from "../session";
import * as newContract from "../newContract";

export type DocumentObject = { fileName: string, region: Region, content: string };

export type Document = string | DocumentObject;

export type InitialSessionState = {
    project?: Project,
    openDocument?: Document,
    documentsToInclude?: Document[]
};

export class Session implements ISession {
    public onCanRunChanged(changed: (canRun: boolean) => void): void {
        if (changed) {
            changed(this.canRun);
        }

        this.canRunChangedCallbacks.push(changed);
    }

    private requestIdGenerator: RequestIdGenerator;
    private workspace: Workspace;
    private executionService: executionService;
    private textEditor: MonacoTextEditor;
    private busReady: boolean;
    private mergedTextChangedEvents: Subject<TextChangedEvent>;
    private serviceErrorChannel = new Subject<ServiceError>();
    private canRunChangedCallbacks: Array<(canRun: boolean) => void> = [];
    private canRun: boolean = false;

    private DispatchRunChanged(canRun: boolean) {
        if (this.canRun === canRun) {
            return;
        }

        this.canRun = canRun;

        for (let callback of this.canRunChangedCallbacks) {
            if (callback) {
                callback(this.canRun);
            }
        }
    }

    constructor(private messageBus: IMessageBus) {
        if (!this.messageBus) {
            throw new Error("messageBus cannot be null");
        }

        this.busReady = false;


        this.requestIdGenerator = new RequestIdGenerator();
        this.executionService = new executionService(this.messageBus, this.requestIdGenerator, this.serviceErrorChannel);
        this.textEditor = new MonacoTextEditor(this.messageBus, this.requestIdGenerator);

        let textChangedHandler = ((event: TextChangedEvent) => {
            if (this.workspace) {
                const document = this.workspace.getOpenDocument();
                if (document && document.id().equal(event.documentId)) {
                    document.setContent(event.text);
                }


            }
        }).bind(this);

        this.mergedTextChangedEvents = new Subject<TextChangedEvent>();
        this.mergedTextChangedEvents.pipe(debounceTime(1000)).subscribe({
            next: (event) => {
                textChangedHandler(event);
            }
        });
    }
    enableLogging(enableLogging: boolean): void {
        const request: newContract.EnableLogging = {
            type: newContract.EnableLoggingType,
            enableLogging: enableLogging
        };
        this.messageBus.post(request);
    }

    private areBussesReady(): boolean {
        return this.busReady;
    }

    private requiresWorkspace(message?: string) {
        if (!this.workspace) {
            throw new Error(message ? message : "workspace cannot be null");
        }
    }


    public getTextEditor(): ITextEditor {
        return this.textEditor;
    }

    public getRequestIdGenerator(): IRequestIdGenerator {
        return this.requestIdGenerator;
    }

    public getMessageBus(): IMessageBus {
        return this.messageBus;
    }

    public subscribeToOutputEvents(handler: OutputEventSubscriber): Unsubscribable {
        return this.executionService.subscribe(handler);
    }
    public subscribeToServiceErrorEvents(handler: ServiceErrorSubscriber): Unsubscribable {
        return this.serviceErrorChannel.subscribe({
            error:
                (error: any) => handler(error)
        });
    }

    configureAndInitialize(configuration: Configuration, initialState?: InitialSessionState): Promise<void> {
        const configureAndInitializePromiseHandler = ((resolve: () => void, reject: (error: any) => void) => {
            if (this.areBussesReady()) {
                this._configureAndInitialize(configuration, initialState)
                    .then(() => {
                        this.textEditor.textChanges.subscribe(this.mergedTextChangedEvents);
                        resolve();
                    })
                    .catch((error: any) => reject(error));
            }
        }).bind(this);

        return new Promise<void>((resolve, reject) => {
            configureAndInitializePromiseHandler(resolve, reject);

            let listenerHandler = ((message: ApiMessage) => {
                message;//?
                if (message.type === HOST_EDITOR_READY_EVENT) {
                    this.busReady = true;
                    configureAndInitializePromiseHandler(resolve, reject);
                }

                if (message.type == HOST_RUN_READY_EVENT) {
                    this.DispatchRunChanged(true);
                }
            }).bind(this);

            this.messageBus.subscribe({ next: message => listenerHandler(message) });

        });
    }

    async _configureAndInitialize(configuration: Configuration, initialState?: InitialSessionState): Promise<void> {

        if (configuration.editorConfiguration) {
            this.textEditor.configure(configuration.editorConfiguration);
        }

        if (initialState) {
            await this.handleInitialState(initialState);
        }

        this.messageBus.post({ type: SHOW_EDITOR_REQUEST });

    }

    private async handleInitialState(initialState: InitialSessionState): Promise<void> {
        await this.handleInitialProject(initialState);
    }


    private async handleInitialProject(initialState: InitialSessionState): Promise<boolean> {
        var project = initialState.project;
        if (project) {
            await this.openProject(project);
            await this.handleDocumentsToInclude(initialState.documentsToInclude);
            if (initialState.openDocument) {
                await this.handleInitialisationWithSingleDocument(initialState);
                return true;
            }
        }
        return false;
    }

    private handleDocumentsToInclude(documentsToInclude: Document[]): Promise<IDocument[]> {
        let docs = [];
        if (documentsToInclude != null && documentsToInclude.length > 0) {
            for (let documentToInclude of documentsToInclude) {
                if (isWithRegion(documentToInclude)) {
                    if (!isNullOrUndefined(documentToInclude.content)) {
                        let parameters: OpenDocumentParameters = {
                            fileName: documentToInclude.fileName,
                            region: documentToInclude.region,
                            content: documentToInclude.content,
                        }
                        docs.push(this.openDocument(parameters));
                    }
                }
            }
        }
        return Promise.all(docs);
    }

    private generateOpenDocumentParameters(documentToOpen: Document): OpenDocumentParameters {
        let openDocumentParameters: OpenDocumentParameters = null;
        if (isWithRegion(documentToOpen)) {
            openDocumentParameters = {
                fileName: documentToOpen.fileName,
                region: documentToOpen.region
            };
            if (!isNullOrUndefined(documentToOpen.content)) {
                openDocumentParameters.content = documentToOpen.content;
            }
        }
        else {
            openDocumentParameters = { fileName: documentToOpen };

        }
        return openDocumentParameters;
    }

    private handleInitialisationWithSingleDocument(initialState: InitialSessionState): Promise<IDocument> {
        let parameters: OpenDocumentParameters = this.generateOpenDocumentParameters(initialState.openDocument);

        return this.openDocument(parameters);
    }

    async openProject(project: Project): Promise<void> {
        if (project) {
            this.workspace = new Workspace(this.messageBus, this.requestIdGenerator);
            await this.workspace.fromProject(project);
        }
        else {
            throw new Error("cannot open null project");
        }
        return Promise.resolve();
    }

    async openDocument(parameters: OpenDocumentParameters): Promise<IDocument> {
        this.requiresWorkspace("Cannot open file without loading a project first");
        let editor = this.textEditor;

        let document = await this.workspace.openDocument({
            fileName: parameters.fileName,
            region: parameters.region,
            content: parameters.content,
            textEditor: editor
        });
        return document;
    }

    async run(configuration?: RunConfiguration): Promise<RunResult> {
        this.requiresWorkspace("Cannot run without loading a project first");
        this.DispatchRunChanged(false);
        return this.executionService.run(configuration).then(result => {
            this.DispatchRunChanged(true);
            return result;
        });
    }


    getOpenDocument(): IDocument {
        return this.workspace.getOpenDocument();
    }
}


function isWithRegion(document: Document): document is { fileName: string, region: Region, content: string } {
    return (<{ fileName: string, region: Region, content: string }>document).region !== undefined;
}
