// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import { IRequestIdGenerator } from "./requestIdGenerator";
import { areSameFile, Document, DocumentId } from "./document";
import { IDocument, Region } from "../editableDocument";
import { Project } from "../project";
import { ITrydotnetMonacoTextEditor } from "./monacoTextEditor";
import { isNullOrUndefined, isNullOrUndefinedOrWhitespace } from "../stringExtensions";
import * as dotnetInteractive from '@microsoft/dotnet-interactive';
import { OpenProject } from "../newContract";
import { SET_WORKSPACE_REQUEST } from "../apiMessages";
import { responseFor } from "./responseFor";
import * as newContract from "../newContract";

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
    private _projectItems: dotnetInteractive.ProjectItem[];
    private workspace: IWorkspace;
    private _currentOpenDocument: Document;
    private workspaceIsModified = false;

    constructor(private projectApiMessageBus: IMessageBus, private requestIdGenerator: IRequestIdGenerator) {
        if (!this.projectApiMessageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }
    }

    public isModified(): boolean {
        return this.workspaceIsModified || (this._currentOpenDocument?.isModified === true);
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

        this.workspaceIsModified = true;

        let requestId = await this.requestIdGenerator.getNewRequestId();
        let wsr = this.toSetWorkspaceRequests();
        if (wsr.workspace) { // && wsr.workspace.buffers && wsr.workspace.buffers.length > 0) {

            let request: any = {
                type: SET_WORKSPACE_REQUEST,
                requestId: requestId,
                workspace: wsr.workspace
            }
            let messageBus = this.projectApiMessageBus;


            let projectOpenedPromise = responseFor(messageBus, dotnetInteractive.ProjectOpenedType, requestId, response => {

                return response;
            });

            messageBus.post(request);

            let projectOpened = <newContract.ProjectOpened><any>(await projectOpenedPromise); //?

            this.setProjectItems(projectOpened.projectItems);

        }
    }

    private findFile(fileName: string): IWorkspaceFile {
        let file = this.workspace.files.find(f => areSameFile(f.name, fileName));//?
        return file;
    }

    private async createDocumentFromSourceFileRegion(file: IWorkspaceFile, regionName: string): Promise<Document> {
        this._projectItems;//?
        if (this._projectItems.length > 0) {
            let item = this._projectItems.find(i => areSameFile(i.relativeFilePath, file.name) && Object.defineProperty(i.regionsContent, regionName, {}));
            if (item) {
                let doc = new Document({ relativeFilePath: item.relativeFilePath, regionName }, item.regionsContent[regionName] ?? "");
                return Promise.resolve(doc);
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
        let id = fileName;



        let document: Document = null;

        if (region) {
            let file = this.findFile(fileName);
            if (file) {
                if (!isNullOrUndefined(content)) {
                    document = new Document({ relativeFilePath: file.name, regionName: region }, content);
                } else {
                    document = await this.createDocumentFromSourceFileRegion(file, region);
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

        let document = await this.createDocument(fileName, region, content);
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

        this.workspaceIsModified = true;

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
            this.workspaceIsModified = true;
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

    public toSetWorkspaceRequests(): SetWorkspaceRequests {

        if (this._currentOpenDocument) {
            this.workspace.buffers = [{
                id: this._currentOpenDocument.id().toString(),
                content: this._currentOpenDocument.getContent(),
                position: this._currentOpenDocument.getCursorPosition()
            }];
        } else {
            this.workspace.buffers = [];
        }

        this.workspaceIsModified = false;
        let requests: SetWorkspaceRequests = {
            workspace: JSON.parse(JSON.stringify(this.workspace))
        };

        let activeDocuments = this.getActiveDocuments();
        for (let activeDocument of activeDocuments) {
            requests.workspace.activeBufferId = activeDocument.document.id().toString();
            activeDocument.document.isModified = false;
        }
        return requests;
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

        this.workspaceIsModified = false;
        let request: OpenProject = {
            type: dotnetInteractive.OpenProjectType,
            project: <dotnetInteractive.Project>{
                files: this.workspace.files.map<dotnetInteractive.ProjectFile>(f => ({ relativeFilePath: f.name, content: f.text })),
            },
            requestId: "",
            editorId: ""
        };

        return request;
    }

    setProjectItems(projectItems: dotnetInteractive.ProjectItem[]) {
        this._projectItems = projectItems ? [...projectItems] : []//?
        this.workspaceIsModified = false;
    }
}


