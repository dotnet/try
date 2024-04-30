// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import { IRequestIdGenerator } from "./requestIdGenerator";
import { Document } from "./document";
import { IDocument, Region } from "../editableDocument";
import { Project } from "../project";
import { ITrydotnetMonacoTextEditor } from "./monacoTextEditor";
import { isNullOrUndefined, isNullOrUndefinedOrWhitespace } from "../stringExtensions";
import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';
import { OpenProject } from "../newContract";
import { responseFor } from "./responseFor";
import * as newContract from "../newContract";
import { areSameFile, DocumentId } from "../documentId";

//todo : this file should go as internal implementation will not user the following types
interface IWorkspace {
    workspaceType: string;
    language?: string;
    files?: IWorkspaceFile[];
    buffers?: IWorkspaceBuffer[];
    usings?: string[];
    activeBufferId?: string;
}

interface IWorkspaceFile {
    name: string;
    text: string;
}

interface IWorkspaceBuffer {
    id: string;
    content: string;
    position: number;
}

export type ActiveDocumentList = {
    editorId: string,
    document: Document
}[];

export type SetWorkspaceRequests = {
    workspace: IWorkspace
};

export class Workspace {
    private _projectItems: polyglotNotebooks.ProjectItem[];
    private workspace: IWorkspace;
    private _currentOpenDocument: Document;

    constructor(private projectApiMessageBus: IMessageBus, private requestIdGenerator: IRequestIdGenerator) {
        if (!this.projectApiMessageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }
    }

    public isModified(): boolean {
        return (this._currentOpenDocument?.isModified === true);
    }

    public async fromProject(project: Project): Promise<void> {

        this._currentOpenDocument = null;
        this.workspace = {
            workspaceType: project.package
        };

        if (!isNullOrUndefinedOrWhitespace(project.language)) {
            this.workspace.language = project.language;
        }

        if (project.usings) {
            this.workspace.usings = JSON.parse(JSON.stringify(project.usings));
        }

        if (project.files) {
            this.workspace.files = project.files.map(f => ({ name: f.name, text: f.content }));
        }


        let requestId = await this.requestIdGenerator.getNewRequestId();
        let prjr = this.toOpenProjectRequests();
        if (prjr.project) { // && wsr.workspace.buffers && wsr.workspace.buffers.length > 0) {

            let request: newContract.OpenProject = {
                type: polyglotNotebooks.OpenProjectType,
                requestId: requestId,
                project: prjr.project,
            }

            let messageBus = this.projectApiMessageBus;

            let projectOpenedPromise = responseFor<newContract.ProjectOpened>(messageBus, polyglotNotebooks.ProjectOpenedType, requestId, response => {

                return <newContract.ProjectOpened>response;
            });

            messageBus.post(request);

            let projectOpened = await projectOpenedPromise; //?

            this.setProjectItems(projectOpened.projectItems);
        }
    }

    private findFile(fileName: string): IWorkspaceFile {
        let file = this.workspace.files.find(f => areSameFile(f.name, fileName));//?
        return file;
    }

    private createDocumentFromSourceFileRegion(file: IWorkspaceFile, regionName: string): Document {
        this._projectItems;//?
        if (this._projectItems.length > 0) {
            let item = this._projectItems.find(i => areSameFile(i.relativeFilePath, file.name) && Object.defineProperty(i.regionsContent, regionName, {}));
            if (item) {
                let doc = new Document({ relativeFilePath: item.relativeFilePath, regionName }, item.regionsContent[regionName] ?? "");
                return doc;
            }
            else {
                throw new Error("Could not find file in project");
            }
        }
        else {
            throw new Error("No project items found");
        }
    }

    private async createDocument(fileName: string, region: Region, content: string): Promise<Document> {
        let document: Document = null;

        if (region) {
            let file = this.findFile(fileName);
            if (file) {
                if (!isNullOrUndefined(content)) {
                    document = new Document({ relativeFilePath: file.name, regionName: region }, content);
                } else {
                    document = this.createDocumentFromSourceFileRegion(file, region);
                }
            }
        }
        else {
            let file = this.findFile(fileName);
            if (file) {
                document = new Document({ relativeFilePath: file.name, regionName: region }, file.text);
            }
        }

        if (!document) {
            document = new Document({ relativeFilePath: fileName, regionName: region }, "");
        }

        if (!isNullOrUndefined(content)) {
            await document.setContent(content);
        }

        return document;
    }

    private async createAndOpenDocument(fileName: string, region: Region, content: string): Promise<Document> {

        const requestId = await this.requestIdGenerator.getNewRequestId();
        let openDocumentResponse = responseFor<newContract.DocumentOpened>(this.projectApiMessageBus, polyglotNotebooks.DocumentOpenedType, requestId, reponse => {
            const od: newContract.DocumentOpened = <newContract.DocumentOpened><any>reponse;
            return od;
        });
        const openDocumentRequest: newContract.OpenDocument = {
            type: polyglotNotebooks.OpenDocumentType,
            relativeFilePath: fileName,
            regionName: region,
            requestId: requestId
        };

        this.projectApiMessageBus.post(openDocumentRequest);
        let documentOpened = await openDocumentResponse;
        let document = await this.createDocument(fileName, region, content ?? documentOpened.content);
        if (document) {
            this._currentOpenDocument = document;
        }

        return document;
    }

    private closeDocumentBeforeOpen(id: DocumentId) {
        if (this._currentOpenDocument && this._currentOpenDocument.id().equal(id)) {
            this._currentOpenDocument.unbindFromEditor();
            this._currentOpenDocument = null;
        }
    }


    public getOpenDocument(): IDocument {
        return this._currentOpenDocument;
    }


    private async _openDocument(fileName: string, region: Region, content: string): Promise<Document> {
        if (!fileName) {
            throw new Error("file cannot be null");
        }
        const id = new DocumentId({ relativeFilePath: fileName, regionName: region });

        let document: Document = DocumentId.areEqual(this._currentOpenDocument?.id(), id) ? this._currentOpenDocument : null;
        // already open document return it
        if (!document) {
            this.closeDocumentBeforeOpen(id);
            document = await this.createAndOpenDocument(fileName, region, content);
        } else if (content) {
            await document.setContent(content);
        }
        return document;
    }

    private getActiveDocuments(): ActiveDocumentList {

        let ret: {
            editorId: string,
            document: Document
        }[] = [];
        if (this._currentOpenDocument && this._currentOpenDocument.isActiveInEditor()) {
            ret.push({
                document: this._currentOpenDocument,
                editorId: this._currentOpenDocument.currentEditor().id()
            });

        }
        return ret;
    }

    public async openDocument(parameters: { fileName: string, region?: Region, content?: string, textEditor?: ITrydotnetMonacoTextEditor }): Promise<Document> {
        let document = await this._openDocument(parameters.fileName, parameters.region, parameters.content);
        if (parameters.textEditor) {
            //  this.unbindAllDocumentsForEditorId(parameters.textEditor.id());
        }
        await document.bindToEditor(parameters.textEditor);
        return document;
    }

    public toOpenProjectRequests(): OpenProject {
        if (this._currentOpenDocument) {
            this.workspace.buffers = [{
                id: this._currentOpenDocument.id().toString(),
                content: this._currentOpenDocument.getContent(),
                position: this._currentOpenDocument.getCursorPosition()
            }];
        }
        else {
            this.workspace.buffers = [];
        }

        let request: OpenProject = {
            type: polyglotNotebooks.OpenProjectType,
            project: <polyglotNotebooks.Project>{
                files: this.workspace.files.map<polyglotNotebooks.ProjectFile>(f => ({ relativeFilePath: f.name, content: f.text })),
            },
            requestId: "",
            editorId: ""
        };

        return request;
    }

    setProjectItems(projectItems: polyglotNotebooks.ProjectItem[]) {
        this._projectItems = projectItems ? [...projectItems] : []//?
    }
}


