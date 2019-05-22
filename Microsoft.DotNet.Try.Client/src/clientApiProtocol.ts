// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type Project = {
    projectTemplate: string,
    usings?: string[],
    files: SourceFile[],
    [key: string]: any
};

export type SourceFile = {
    name: string,
    content: string,
};

export type SourceFileRegion = {
    id: string,
    content: string,
};


export interface MessageBase {
    requestId: string;
}

export interface CreateProjectRequest extends MessageBase {
    projectTemplate: string;
}

export interface CreateProjectResponse extends MessageBase {
    project: Project;
}

export interface CreateProjectFromGistRequest extends CreateProjectRequest {
    gistId: string;
    commitHash?: string;
}

export interface CreateRegionsFromFilesRequest extends MessageBase {
    files: SourceFile[];
}

export interface CreateRegionsFromFilesResponse extends MessageBase {
    regions: SourceFileRegion[];
}
