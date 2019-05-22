// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type Project = {
    package: string,
    packageVersion?:string,
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

export function createProject(packageName: string, files: SourceFile[], usings?: string[]): Promise<Project> {
    if (!packageName || packageName.length === 0) {
        throw new Error("packageName can not be null or empty");
    }

    if (!files || files.length === 0) {
        throw new Error("at least a file is required");
    }

    let project: Project = {
        package: packageName,
        files: JSON.parse(JSON.stringify(files))
    };

    if (usings) {
        project.usings = JSON.parse(JSON.stringify(usings));
    }

    return Promise.resolve(project);
}
