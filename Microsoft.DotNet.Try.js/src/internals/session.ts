// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SignatureHelpResult } from "../signatureHelp";
import { IMessageBus } from "./messageBus";
import { Configuration } from "../configuration";
import { RequestIdGenerator, IRequestIdGenerator } from "./requestIdGenerator";
import { Workspace } from "./workspace";
import { Region, IDocument } from "../editableDocument";
import { signatureHelpService } from "./intellisense/signatureHelpService";
import { completionListService } from "./intellisense/completionListService";
import { compilationService } from "./execution/compilationService";
import { executionService } from "./execution/executionService";
import { SHOW_EDITOR_REQUEST, HOST_EDITOR_READY_EVENT, HOST_RUN_READY_EVENT, ApiMessage, SET_WORKSPACE_REQUEST } from "./apiMessages";
import { MonacoTextEditor } from "./monacoTextEditor";
import { ITextEditor, TextChangedEvent } from "../editor";
import { Project } from "../project";
import { CompletionResult } from "../completion";
import { debounceTime } from "rxjs/operators";
import { Subject, Unsubscribable } from "rxjs";
import { isNullOrUndefined, isNullOrUndefinedOrWhitespace } from "../stringExtensions";
import { ServiceError, ISession, OutputEventSubscriber, ServiceErrorSubscriber, OpenDocumentParameters, RunConfiguration, RunResult, CompilationResult } from "../session";

export type DocumentObject = { fileName: string, region: Region, content: string };
export type Document = string | DocumentObject;
export type DocumentsToOpen = { [key: string]: Document };
export type InitialSessionState = {
    project?: Project,
    openDocument?: Document,
    openDocuments?: DocumentsToOpen
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
    private signatureHelpService: signatureHelpService;
    private completionListService: completionListService;
    private compilationService: compilationService;
    private executionService: executionService;
    private textEditors: MonacoTextEditor[];
    private busReady: boolean[] = [];
    private mergedTextChangedEvents: Subject<TextChangedEvent>;
    private runAsCodeChanges: boolean = false;
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

    constructor(private messageBuses: IMessageBus[]) {
        if (!this.messageBuses || this.messageBuses.length < 1) {
            throw new Error("messageBuses cannot be null and must have at least one bus");
        }

        for (let i = 0; i < this.messageBuses.length; i++) {
            this.busReady.push(false);
        }

        this.requestIdGenerator = new RequestIdGenerator(this.messageBuses[0]);
        this.signatureHelpService = new signatureHelpService(this.messageBuses[0], this.requestIdGenerator, this.serviceErrorChannel);
        this.completionListService = new completionListService(this.messageBuses[0], this.requestIdGenerator, this.serviceErrorChannel);
        this.compilationService = new compilationService(this.messageBuses[0], this.requestIdGenerator, this.serviceErrorChannel);
        this.executionService = new executionService(this.messageBuses[0], this.requestIdGenerator, this.serviceErrorChannel);
        this.textEditors = this.messageBuses.map((messageBus) => new MonacoTextEditor(messageBus, this.requestIdGenerator, messageBus.id()));

        let textChangedHandler = (() => {
            this.setWorkspaceIfRequired();
            if (this.runAsCodeChanges) {
                this.run();
            }
        }).bind(this);

        this.mergedTextChangedEvents = new Subject<TextChangedEvent>();
        this.mergedTextChangedEvents.pipe(debounceTime(1000)).subscribe((_next) => {
            textChangedHandler();
        });
    }

    private areBussesReady(): boolean {
        return this.busReady.reduce((prev, current) => prev && current, true)
    }

    private requiresWorkspace(message?: string) {
        if (!this.workspace) {
            throw new Error(message ? message : "workspace cannot be null");
        }
    }

    private setWorkspaceIfRequired(): Promise<void> {
        if (this.workspace && this.workspace.isModified) {
            return this.setWorkspace();
        }
    }

    private async setWorkspace(): Promise<void> {
        if (this.workspace) {
            let requestId = await this.requestIdGenerator.getNewRequestId();
            let wsr = this.workspace.toSetWorkspaceRequests();
            if (wsr.workspace && wsr.workspace.buffers && wsr.workspace.buffers.length > 0) {
                let editorIds = Object.getOwnPropertyNames(wsr.bufferIds);
                for (let editorId of editorIds) {
                    let editor = <MonacoTextEditor>(this.getTextEditorById(editorId));
                    let messageBus = editor.messageBus();
                    messageBus.post({
                        type: SET_WORKSPACE_REQUEST,
                        requestId: requestId,
                        workspace: wsr.workspace,
                        bufferId: wsr.bufferIds[editorId]
                    })
                }
            }
        }
    }

    public getTextEditor(): ITextEditor {
        return this.textEditors[0];
    }

    public getTextEditors(): ITextEditor[] {
        return this.textEditors;
    }

    public getTextEditorById(editorId: string): ITextEditor {
        return this.textEditors.find(editor => editor.id() === editorId);
    }

    public getRequestIdGenerator(): IRequestIdGenerator {
        return this.requestIdGenerator;
    }

    public getMessageBus(): IMessageBus {
        return this.messageBuses[0];
    }

    public subscribeToOutputEvents(handler: OutputEventSubscriber): Unsubscribable {
        return this.executionService.subscribe(handler);
    }
    public subscribeToServiceErrorEvents(handler: ServiceErrorSubscriber): Unsubscribable {
        return this.serviceErrorChannel.subscribe(error => handler(error));
    }

    configureAndInitialize(configuration: Configuration, initialState?: InitialSessionState): Promise<void> {
        const configureAndInitializePromiseHandler = ((resolve: () => void, reject: (error: any) => void) => {
            if (this.areBussesReady()) {
                this._configureAndInitialize(configuration, initialState)
                    .then(() => {
                        for (let editor of this.textEditors) {
                            editor.textChanges.subscribe(this.mergedTextChangedEvents);
                        }
                        this.setWorkspace();
                        resolve();
                    })
                    .catch((error: any) => reject(error));
            }
        }).bind(this);

        return new Promise<void>((resolve, reject) => {
            configureAndInitializePromiseHandler(resolve, reject);

            let listenerHandler = ((message: ApiMessage, busId: number) => {
                if (message.type === HOST_EDITOR_READY_EVENT) {
                    this.busReady[busId] = true;
                    configureAndInitializePromiseHandler(resolve, reject);
                }

                if (message.type == HOST_RUN_READY_EVENT) {
                    this.DispatchRunChanged(true);
                }
            }).bind(this);

            for (let i = 0; i < this.messageBuses.length; i++) {
                this.messageBuses[i].subscribe(message => listenerHandler(message, i));
            }
        });
    }

    async _configureAndInitialize(configuration: Configuration, initialState?: InitialSessionState): Promise<void> {

        if (configuration.editorConfiguration) {
            for (let editor of this.textEditors) {
                editor.configure(configuration.editorConfiguration);
            }
        }

        this.runAsCodeChanges = configuration.runAsCodeChanges === true;

        if (initialState) {
            await this.handleInitialState(initialState);
        }

        for (let messageBus of this.messageBuses) {
            messageBus.post({ type: SHOW_EDITOR_REQUEST });
        }
    }

    private async handleInitialState(initialState: InitialSessionState): Promise<void> {
        let loadedProject = await this.handleInitialProject(initialState);
    }


    private async handleInitialProject(initialState: InitialSessionState): Promise<boolean> {
        var project = initialState.project;
        if (project) {
            await this.openProject(project);
            await this.handleDocumentsToInclude(initialState.documentsToInclude);
            if (initialState.openDocument) {
                await this.handleInitialisationWithSingleDocument(initialState);
                return true;
            } else if (initialState.openDocuments) {
                await this.handleInitialisationWithMultipleDocument(initialState);
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
        parameters.editorId = this.textEditors[0].id();
        return this.openDocument(parameters);
    }

    private handleInitialisationWithMultipleDocument(initialState: InitialSessionState): Promise<IDocument[]> {
        let editorIds = Object.getOwnPropertyNames(initialState.openDocuments);
        let docs = [];
        for (let editorId of editorIds) {
            let documentToOpen = initialState.openDocuments[editorId];
            let openDocumentParameters: OpenDocumentParameters = this.generateOpenDocumentParameters(documentToOpen);
            openDocumentParameters.editorId = editorId;
            docs.push(this.openDocument(openDocumentParameters));
        }
        return Promise.all(docs);
    }

    openProject(project: Project): Promise<void> {
        if (project) {
            this.workspace = new Workspace(this.messageBuses[0], this.requestIdGenerator);
            this.workspace.fromProject(project);
        }
        else {
            throw new Error("cannot open null project");
        }
        return Promise.resolve();
    }

    async openDocument(parameters: OpenDocumentParameters): Promise<IDocument> {
        this.requiresWorkspace("Cannot open file without loading a project first");
        let editor = isNullOrUndefinedOrWhitespace(parameters.editorId) ? undefined : <MonacoTextEditor>(this.getTextEditorById(parameters.editorId));
        let document = await this.workspace.openDocument({
            fileName: parameters.fileName,
            region: parameters.region,
            content: parameters.content,
            textEditor: editor
        });
        await this.setWorkspaceIfRequired();
        return document;
    }

    async run(configuration?: RunConfiguration): Promise<RunResult> {
        this.requiresWorkspace("Cannot run without loading a project first");
        this.DispatchRunChanged(false);
        await this.setWorkspaceIfRequired();
        return this.executionService.run(this.workspace, configuration).then(result => {
            this.DispatchRunChanged(true);
            return result;
        });
    }
    async compile(): Promise<CompilationResult> {
        this.requiresWorkspace("Cannot compile without loading a project first");
        await this.setWorkspaceIfRequired();
        return this.compilationService.compile(this.workspace);
    }

    async getSignatureHelp(fileName: string, position: number, region?: Region): Promise<SignatureHelpResult> {
        this.requiresWorkspace("Cannot get SignatureHelp without loading a project first");
        await this.setWorkspaceIfRequired();
        return this.signatureHelpService.getSignatureHelp(this.workspace, fileName, position, region);
    }

    async getCompletionList(fileName: string, position: number, region?: Region): Promise<CompletionResult> {
        this.requiresWorkspace("Cannot get CompletionList without loading a project first");
        await this.setWorkspaceIfRequired();
        return this.completionListService.getCompletionList(this.workspace, fileName, position, region);
    }

    getOpenDocuments(): IDocument[]{
        return this.workspace.getAllOpenDocuments();
    }
}


function isWithRegion(document: Document): document is { fileName: string, region: Region, content: string } {
    return (<{ fileName: string, region: Region, content: string }>document).region !== undefined;
}
