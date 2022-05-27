// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import * as dotnetInteractive from '@microsoft/dotnet-interactive';
export const EditorContentChangedType = "EditorContentChanged";
export interface ProjectOpened {
    type: typeof dotnetInteractive.ProjectOpenedType;
    projectItems: dotnetInteractive.ProjectItem[];
    requestId: string;
    editorId: string;
}

export interface OpenProject {
    type: typeof dotnetInteractive.OpenProjectType;
    requestId: string;
    editorId: string;
    project: dotnetInteractive.Project
}

export interface DocumentOpened {
    type: typeof dotnetInteractive.DocumentOpenedType;
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
    type: typeof dotnetInteractive.OpenDocumentType;
    content: string;
    relativeFilePath: string;
    regionName?: string;
    editorId: string;
} 