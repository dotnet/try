// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "./messageBus";
import { IRequestIdGenerator } from "./requestIdGenerator";
import { Document } from "./document";
import { Region } from "../editableDocument";
import { responseFor } from "./responseFor";
import { CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE, CREATE_REGIONS_FROM_SOURCEFILES_REQUEST } from "./apiMessages";
import { Project, SourceFileRegion } from "../project";
import { ITrydotnetMonacoTextEditor } from "./monacoTextEditor";
import { isNullOrUndefined, isNullOrUndefinedOrWhitespace } from "../stringExtensions";

export interface IWorkspace {
    workspaceType: string;
    language?: string;
    files?: IWorkspaceFile[];
    buffers?: IWorkspaceBuffer[];
    usings?: string[];
    includeInstrumentation?: boolean;
}

export interface IWorkspaceFile {
    name: string;
    text: string;
}

export interface IWorkspaceBuffer {
    id: string;
    content: string;
    position: number;
}

export type ActiveDocumentList = {
    editorId: string,
    document: Document
}[];

export type SetWorkspaceRequests = {
    workspace: IWorkspace,
    bufferIds: {
        [key: string]: string
    }
};

export class Workspace {
    private packageVersion: string;
    private workspace: IWorkspace;
    private openDocuments: { [name: string]: Document } = {};
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
        return this.workspaceIsModified || (this.openDocuments && this.getAllOpenDocuments().some(d => d.isModified));
    }

    public fromProject(project: Project): void {

        this.openDocuments = {};
        this.workspace = {
            workspaceType: project.package       
        };

        if(!isNullOrUndefinedOrWhitespace(project.language)){
            this.workspace.language = project.language;
        }

        if (project.usings) {
            this.workspace.usings = JSON.parse(JSON.stringify(project.usings));
        }

        if (project.files) {
            this.workspace.files = project.files.map(f => ({ name: f.name, text: f.content }));
        }

        if (!isNullOrUndefinedOrWhitespace(project.packageVersion)) {
            this.packageVersion = project.packageVersion;
        }

        this.workspaceIsModified = true;
    }

    private findFile(fileName: string): IWorkspaceFile {
        let file = this.workspace.files.find(f => f.name === fileName);
        return file;
    }

    private findRegion(regions: SourceFileRegion[], id: string): SourceFileRegion {
        return regions ? regions.find(p => p.id === id) : null;
    }

    private async createDocumentFromSourceFileRegion(file: IWorkspaceFile, id: string): Promise<Document> {
        const requestId = await this.requestIdGenerator.getNewRequestId();
        let ret = responseFor<Document>(this.projectApiMessageBus, CREATE_REGIONS_FROM_SOURCEFILES_RESPONSE, requestId, (responseMessage) => {
            let result: Document = null;
            let regions = <SourceFileRegion[]>((<any>responseMessage).regions);
            let region = this.findRegion(regions, id);
            if (region) {
                result = new Document(region.id, region.content);
            }
            return result;
        });

        this.projectApiMessageBus.post({
            type: CREATE_REGIONS_FROM_SOURCEFILES_REQUEST,
            requestId: requestId,
            files: [{ name: file.name, content: file.text }]
        });

        return ret;
    }

    private async createDocument(fileName: string, region: Region, content: string): Promise<Document> {
        let id = fileName;

        if (region) {
            id = `${fileName}@${region}`;
        }

        let document: Document = null;

        if (region) {
            let file = this.findFile(fileName);
            if (file) {
                if (!isNullOrUndefined(content)) {
                    document = new Document(id, content);
                } else {
                    document = await this.createDocumentFromSourceFileRegion(file, id);
                }
            }
        }
        else {
            let file = this.findFile(id);
            if (file) {
                document = new Document(id, file.text);
            }
        }

        if (!document) {
            document = new Document(id, "");
        }

        if (!isNullOrUndefined(content)) {
            await document.setContent(content);
        }

        return document;
    }

    private async createAndOpenDocument(fileName: string, region: Region, content: string): Promise<Document> {
        let id = fileName;
        if (region) {
            id = `${fileName}@${region}`;
        }
        let document = await this.createDocument(fileName, region, content);
        if (document) {
            this.openDocuments[id] = document;
        }

        return document;
    }

    private closeDocumentBeforeOpen(fileName: string, region?: Region) {
        if (region && this.openDocuments[fileName]) {
            this.openDocuments[fileName].unbindFromEditor();
            delete this.openDocuments[fileName];
            this.workspaceIsModified = true;
        }
        else if (!region) {
            this.closeAllOpenDocumentsForFile(fileName);
            this.workspaceIsModified = true;
        }
    }

    private closeAllOpenDocumentsForFile(fileName: string) {
        let toDelete = Object.getOwnPropertyNames(this.openDocuments).filter(name => name.startsWith(`${fileName}@`));
        for (var region of toDelete) {
            this.openDocuments[region].unbindFromEditor();
            delete this.openDocuments[region];
        }
    }

    private unbindAllDocumentsForEditorId(editorId: string) {
        let docIds = Object.getOwnPropertyNames(this.openDocuments);
        let activeDocuments = docIds
            .map(id => this.openDocuments[id])
            .filter(openDocument => openDocument.isActiveInEditor() && openDocument.currentEditor().id() === editorId);

        for (let activeDocument of activeDocuments) {
            activeDocument.unbindFromEditor();
        }
    }

    public getAllOpenDocuments() {
        return Object.getOwnPropertyNames(this.openDocuments).map(n => this.openDocuments[n]);
    }


    private async _openDocument(fileName: string, region: Region, content: string): Promise<Document> {
        if (!fileName) {
            throw new Error("file cannot be null");
        }

        let id = region ? `${fileName}@${region}` : fileName;

        let document: Document = this.openDocuments[id];
        // already open document return it
        if (!document) {
            this.closeDocumentBeforeOpen(fileName, region);
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
        }[] = null;
        ret = this.getAllOpenDocuments().filter(d => d.isActiveInEditor()).map(d => {
            return {
                document: d,
                editorId: d.currentEditor().id()
            }
        });
        return ret;
    }

    public async openDocument(parameters: { fileName: string, region?: Region, content?: string, textEditor?: ITrydotnetMonacoTextEditor }): Promise<Document> {
        let document = await this._openDocument(parameters.fileName, parameters.region, parameters.content);
        if (parameters.textEditor) {
            this.unbindAllDocumentsForEditorId(parameters.textEditor.id());
        }
        await document.bindToEditor(parameters.textEditor);
        return document;
    }

    public toSetWorkspaceRequests(): SetWorkspaceRequests {
        this.workspace.buffers = this.getAllOpenDocuments().map<IWorkspaceBuffer>(d => ({
            id: d.id(),
            content: d.getContent(),
            position: d.getCursorPosition()
        }));

        this.workspaceIsModified = false;
        let requests: SetWorkspaceRequests = {
            workspace: JSON.parse(JSON.stringify(this.workspace)),
            bufferIds: {}
        };

        let activeDocuments = this.getActiveDocuments();
        for (let activeDocument of activeDocuments) {
            requests.bufferIds[activeDocument.editorId] = activeDocument.document.id()
        }

        return requests;
    }
}
