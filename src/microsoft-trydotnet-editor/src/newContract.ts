// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';
export const EditorContentChangedType = "EditorContentChanged";
export const ConfigureMonacoEditorType = "ConfigureMonacoEditor";
export const SetMonacoEditorSizeType = "SetMonacoEditorSize";
export const DefineMonacoEditorThemesType = "DefineMonacoEditorThemes";
export const SetEditorContentType = "SetEditorContent";
export const EnableLoggingType = "EnableLogging";

export interface EnableLogging {
    type: typeof EnableLoggingType;
    enableLogging: boolean;
}
export interface ProjectOpened {
    type: typeof polyglotNotebooks.ProjectOpenedType;
    projectItems: polyglotNotebooks.ProjectItem[];
    requestId: string;
    editorId: string;
}

export interface OpenProject {
    type: typeof polyglotNotebooks.OpenProjectType;
    requestId: string;
    editorId?: string;
    project: polyglotNotebooks.Project
}

export interface DocumentOpened {
    type: typeof polyglotNotebooks.DocumentOpenedType;
    requestId: string;
    editorId: string;
    content: string;
    relativeFilePath: string;
    regionName?: string;
}

export interface EditorContentChanged {
    type: typeof EditorContentChangedType;
    content: string;
    relativeFilePath: string;
    regionName?: string;
    editorId: string;
}

export interface OpenDocument {
    type: typeof polyglotNotebooks.OpenDocumentType;
    relativeFilePath: string;
    regionName?: string;
    editorId?: string;
    requestId: string;
}

export interface SetMonacoEditorSize {
    type: typeof SetMonacoEditorSizeType;
    size: {
        width: number,
        height: number
    }
}

export interface CongureMonacoEditor {
    type: typeof ConfigureMonacoEditorType;
    editorOptions?: any,
    theme?: string
}

export interface DefineMonacoEditorThemes {
    type: typeof DefineMonacoEditorThemesType;
    themes: {
        [key: string]: any
    }
}

export interface SetEditorContent {
    type: typeof SetEditorContentType;
    content: string;
    editorId?: string;
    requestId: string;
}


export function isMessageOfType(message: { type: string, [key: string]: any }, type: string): boolean {
    return message && message.type && type && message.type.toLowerCase() === type.toLowerCase();
}

export function isMessageCorrelatedTo(message: { type: string, [key: string]: any }, requestId: string): boolean {
    return message && requestId && (message).requestId && (message).requestId === requestId;
}