// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDiagnostic, IInstrumentation, IWorkspace, IGistWorkpaceInfo, IVariableLocation } from "./IState";

import { IWorkspaceRequest } from "./IMlsClient";
import * as monacoEditor from "monaco-editor";
import ICompletionItem from "./ICompletionItem";
import { CreateProjectFromGistRequest, CreateProjectResponse, CreateRegionsFromFilesRequest, CreateRegionsFromFilesResponse } from "./clientApiProtocol";
import { ClientConfiguration } from "./clientConfiguration";

export default interface IMlsClient {
    getSourceCode: (from: IWorkspaceRequest) => Promise<IWorkspaceResponse>;
    getWorkspaceFromGist(gistId: string, workspaceType: string, extractBuffers: boolean): Promise<IGistWorkspace>;
    run: (args: IRunRequest) => Promise<IRunResponse>;
    compile: (args: IRunRequest) => Promise<ICompileResponse>;
    acceptCompletionItem: (selection: ICompletionItem) => Promise<void>;
    getCompletionList: (workspace: IWorkspace, bufferId: string, position: number, completionProvider: string) => Promise<CompletionResult>;
    getSignatureHelp: (workspace: IWorkspace, bufferId: string, position: number) => Promise<SignatureHelpResult>;
    getDiagnostics: (workspace: IWorkspace, bufferId: string) => Promise<DiagnosticResult>;
    createProjectFromGist: (request: CreateProjectFromGistRequest) => Promise<CreateProjectResponse>;
    createRegionsFromProjectFiles: (request: CreateRegionsFromFilesRequest) => Promise<CreateRegionsFromFilesResponse>;
    getConfiguration: () => Promise<ClientConfiguration>;
}

export type CompletionResult = {
    items: ICompletionItem[],
    diagnostics: IDiagnostic[]
};

export type SignatureHelpResult = {
    signatures: monacoEditor.languages.SignatureInformation[];
    activeSignature: number;
    activeParameter: number;
    diagnostics: IDiagnostic[]
};

export type DiagnosticResult = {
    diagnostics: IDiagnostic[]
};

export interface IGistWorkspace extends IGistWorkpaceInfo {
    workspace: IWorkspace;
}

export interface IRunRequest {
    workspace: IWorkspace;
    requestId?: string;
    [key: string]: any;
}

export interface IRunResponse {
    exception?: string;
    output?: string[];
    succeeded?: boolean;
    diagnostics?: IDiagnostic[];
    instrumentation?: IInstrumentation[];
    variableLocations?: IVariableLocation[];
    requestId?: string;
}


export interface ICompileResponse {
    succeeded?: boolean;
    diagnostics?: IDiagnostic[];
    base64assembly?: string;
    requestId?: string;
}

export interface IMlsCompletionItem {
    acceptanceUri?: string;
    displayText?: string;
    documentation: string;
    filterText?: string;
    insertText?: string;
    kind?: string;
    sortText?: string;
}

export interface ISignatureHelpResponse {
    signatures: ISignatureHelpItem[];
    activeSignature: number;
    activeParameter: number;
}

export interface ISignatureHelpItem {
    name: string;
    label: string;
    documentation: string;
    parameters: ISignatureHelpParameter[];
}

export interface ISignatureHelpParameter {
    name: string;
    label: string;
    documetation: string;
}

export interface IWorkspaceRequest {
    sourceUri: string;
}

export interface IWorkspaceResponse {
    buffer: string;
}

export interface ApiError {
    code: "TimeoutError" | "ConfigurationVersionError";
}
