// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SourceFile } from "../project";
import { DocumentObject } from "../internals/session";
import { tryDotNetModes, SessionLookup, tryDotNetVisibilityModifiers, tryDotNetRegionInjectionPoints } from "./types";
import { getTrydotnetSessionId, getVisibility } from "./utilities";
import { getCode } from "./codeProcessor";
import { isNullOrUndefinedOrWhitespace } from "../stringExtensions";

export type Includes = {
    global?: {
        files: SourceFile[];
        documents: DocumentObject[];
    };
    [key: string]: {
        files: SourceFile[];
        documents: DocumentObject[];
    };
};

function getOrCreateDocument(sessionId: string, filename: string, region: string, includes: Includes): DocumentObject {
    if (!includes[sessionId]) {
        includes[sessionId] = {
            files: [],
            documents: []
        };
    }

    let sessionIncludes = includes[sessionId];

    let document = sessionIncludes.documents.find((document: DocumentObject) => document.fileName === filename && document.region === region);

    if (!document) {
        document = {
            fileName: filename,
            region: region,
            content: ""
        };
        sessionIncludes.documents.push(document);
    }

    return document;
}

function getOrCreateFile(
    sessionId: string,
    filename: string,
    includes: Includes
): SourceFile {
    if (!includes[sessionId]) {
        includes[sessionId] = {
            files: [],
            documents: []
        };
    }

    let sessionIncludes = includes[sessionId];

    let file = sessionIncludes.files.find(file => file.name === filename);

    if (!file) {
        file = {
            name: filename,
            content: ""
        };
        sessionIncludes.files.push(file);
    }

    return file;
}

export function getDocumentsToInclude(includes: Includes, sessionId: string): DocumentObject[] {
    let merged: DocumentObject[] = [];

    if (includes !== null) {
        if (includes.global && includes.global.documents.length > 0) {
            merged =  merged.concat(includes.global.documents);
        }
        let sessionRelated = includes[sessionId];
        if (sessionRelated && sessionRelated.documents.length > 0) {
            merged =  merged.concat(sessionRelated.documents);
        }
    }
    return merged;
}

export function mergeFiles(
    source: SourceFile[],
    includes: Includes,
    sessionId: string
): SourceFile[] {
    let merged: SourceFile[] = [];
    if (source !== null && source.length > 0) {
        merged =  merged.concat(source);
    }
    if (includes !== null) {
        if (includes.global && includes.global.files.length > 0) {
            merged =  merged.concat(includes.global.files);
        }
        let sessionRelated = includes[sessionId];
        if (sessionRelated && sessionRelated.files.length > 0) {
            merged =  merged.concat(sessionRelated.files);
        }
    }
    return merged;
}

function getOrder(element: HTMLElement): number {
    let order = element.dataset.trydotnetOrder;

    if (order === undefined || order === null) {
        return 0;
    }

    return Number(order);
}

export function extractIncludes(doc: HTMLDocument): Includes {
    consolidateInliningOrder(doc);

    let includes: Includes = {
        global: null
    };

    let sessions: SessionLookup = {};

    let codeSources = doc.querySelectorAll(
        `pre>code[data-trydotnet-mode=${
        tryDotNetModes[tryDotNetModes.include]
        }]`
    );
    let domeElementstoRemove: Node[] = [];
    codeSources.forEach(async (codeSource: HTMLElement) => {
        let sessionId = getTrydotnetSessionId(codeSource, "global");
        if (!sessions[sessionId]) {
            sessions[sessionId] = { codeSources: [] };
        }
        let visibility = getVisibility(codeSource);
        if (visibility === tryDotNetVisibilityModifiers[tryDotNetVisibilityModifiers.hidden]) {
            domeElementstoRemove.push(codeSource.parentNode);
        }
        sessions[sessionId].codeSources.push(codeSource);
    });

    for (let sessionId in sessions) {
        let session = sessions[sessionId];
        for (let codeSource of session.codeSources.sort(elementComparer)) {
            let code = getCode(codeSource);
            let filename = codeSource.dataset.trydotnetFileName;
            if (!filename) {
                filename = `include_file_${sessionId}.cs`;
            }
            let region = codeSource.dataset.trydotnetRegion;
            if (!isNullOrUndefinedOrWhitespace(region)) {
                let injectionPoint = codeSource.dataset.trydotnetInjectionPoint;
                if (isNullOrUndefinedOrWhitespace(injectionPoint)) {
                    injectionPoint = "before";
                }
                let document = getOrCreateDocument(sessionId, filename, `${region}[${injectionPoint}]`, includes);
                document.content = `${document.content}${code}\n`;
            } else {
                let file = getOrCreateFile(sessionId, filename, includes);
                file.content = `${file.content}${code}\n`;
            }
        }
    }

    domeElementstoRemove.forEach(element => {
        element.parentNode.removeChild(element);
    });

    return includes;
}

function consolidateInliningOrder(doc: HTMLDocument) {
    let codeSources = doc.querySelectorAll(
        `pre>code[data-trydotnet-mode]`);

    let codeSourcesBySession: { [key: string]: HTMLElement[] } = {}

    codeSources.forEach((codeSource: HTMLElement) => {
        let sessionId = getTrydotnetSessionId(codeSource);
        if (!codeSourcesBySession[sessionId]) {
            codeSourcesBySession[sessionId] = [];
        }
        codeSourcesBySession[sessionId].push(codeSource);
    });
    for (let sessionId in codeSourcesBySession) {
        let codeSources = codeSourcesBySession[sessionId].sort(elementComparer);
        let editableRegionsFound: { [key: string]: boolean } = {};

        codeSources.forEach((codeSource: HTMLElement) => {
            let region = codeSource.dataset.trydotnetRegion;
            if (!isNullOrUndefinedOrWhitespace(region)) {
                let mode = codeSource.dataset.trydotnetMode;
                let id = computeId(codeSource);
                if (mode === tryDotNetModes[tryDotNetModes.editor]) {
                    editableRegionsFound[id] = true;
                }
                else if (mode === tryDotNetModes[tryDotNetModes.include]) {
                    let injectionPoint = codeSource.dataset.trydotnetInjectionPoint;
                    if (isNullOrUndefinedOrWhitespace(injectionPoint)) {
                        injectionPoint = (!!editableRegionsFound[id] === true)
                            ? tryDotNetRegionInjectionPoints[tryDotNetRegionInjectionPoints.after]
                            : tryDotNetRegionInjectionPoints[tryDotNetRegionInjectionPoints.before];

                        codeSource.dataset.trydotnetInjectionPoint = injectionPoint;
                    }
                }
            }
        });
    }
}

function tryGetFileName(element: HTMLElement) {
    let fileName = element.dataset.trydotnetFileName;
    if (isNullOrUndefinedOrWhitespace(fileName)) {
        fileName = "temp";
    }
    return fileName;
}

function computeId(element: HTMLElement){
    let fileName = tryGetFileName(element);
    let region = element.dataset.trydotnetRegion;
    return `${fileName}@${region}`;
}

function elementComparer(elementA: HTMLElement, elementB: HTMLElement){
    return getOrder(elementA) - getOrder(elementB);
}
