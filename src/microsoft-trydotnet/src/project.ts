import { isNullOrUndefinedOrWhitespace } from "./stringExtensions";

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type Project = {
    package: string,
    packageVersion?: string,
    language?: string,
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

export function createProject(args: { packageName: string, files: SourceFile[], usings?: string[], language?: string }): Promise<Project> {
    if (isNullOrUndefinedOrWhitespace(args.packageName)) {
        throw new Error("packageName can not be null or empty");
    }

    if (!args.files || args.files.length === 0) {
        throw new Error("at least a file is required");
    }

    let project: Project = {
        package: args.packageName,
        files: JSON.parse(JSON.stringify(args.files))
    };

    if (isNullOrUndefinedOrWhitespace(args.language)) {
        project.language = "csharp";
    } else {
        project.language = args.language;
    }

    if (args.usings) {
        project.usings = JSON.parse(JSON.stringify(args.usings));
    }

    return Promise.resolve(project);
}
