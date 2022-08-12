// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface legacyContract {
    requestId: string;
}

export interface CreateProjectRequest extends legacyContract {
    projectTemplate: string;
}

export interface IWorkspace {
    language?: string;
    files?: IWorkspaceFile[];
    buffers: IWorkspaceBuffer[];
    usings?: string[];
    activeBufferId?: string;
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