// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import IMlsClient from "./IMlsClient";
import * as monacoEditor from "monaco-editor";
import { ICodeEditorForTryDotNet } from "./constants/ICodeEditorForTryDotNet";
import { IApplicationInsightsClient } from "./ApplicationInsights";
export default interface IState {
    compile: ICompileState;
    config: IConfigState;
    monaco: IMonacoState;
    run: IRunState;
    ui: IUiState;
    workspace: IWorkspaceState;
    workspaceInfo: IWorkspaceInfo;
    wasmRunner: IWasmRunnerState;
}

export interface ICompileState
{
    succeeded?: boolean;
    diagnostics?: IDiagnostic[];
    base64assembly?: string;
    workspaceVersion: number;
}

export interface IUiState {
    canShowGitHubPanel?: boolean;
    canEdit?: boolean;
    canRun?: boolean;
    showEditor?: boolean;
    [x: string]: any;
    showCompletions?: boolean;
    additionalUsings?: string[];
    workspaceType?: string;
    isRunning?: boolean;
    instrumentationActive?: boolean;
    enableBranding?: boolean;
}

export interface IWasmRunnerState {
    payload: object;
    callback: (obj: any) => void;
    sequence: number;
}

export interface IRunState {
    exception?: string | Error;
    output?: string[];
    fullOutput?: string[];
    succeeded?: boolean;
    [x: string]: any;
    diagnostics?: IDiagnostic[];
    instrumentation?: IInstrumentation[];
    currentInstrumentationStep?: number;
    variableLocations?: IVariableLocation[];
}

export interface IVariableLocation {
    name: string;
    locations: IFileLocationSpan[];
    declaredAt: IVarDeclaration;
}

export interface IFileLocationSpan {
    startLine: number;
    endLine: number;
    startColumn: number;
    endColumn: number;
}

export interface IDiagnostic {
    start: number;
    end: number;
    message: string;
    severity: number;
}

export interface IInstrumentation {
    output?: IInstrumentationOutput;
    filePosition?: IFilePosition;
    stackTrace?: string;
    locals?: IVariable[];
    parameters?: IVariable[];
    fields?: IVariable[];
}

export interface IInstrumentationOutput {
    end: number;
    start: number;
}

export interface IFilePosition {
    character: number;
    line: number;
    file?: string;
}

export interface IVariable {
    name: string;
    value: string;
    declaredAt: IVarDeclaration;
}

export interface IVarDeclaration {
    start: number;
    end: number;
}

export interface IMonacoState {
    editor?: ICodeEditorForTryDotNet;
    editorOptions?: monacoEditor.editor.IEditorOptions;
    displayedCode?: string;
    theme?: string;
    themes?: { [x: string]: monacoEditor.editor.IStandaloneThemeData };
    bufferId?: string;
    [x: string]: any;
}

export interface IConfigState {
    completionProvider?: string;
    client?: IMlsClient;
    from?: string;
    [x: string]: any;
    version?: number;
    defaultWorkspace?: IWorkspace;
    defaultCodeFragment?: string;
    useLocalCodeRunner?: boolean;
    hostOrigin?: URL;
    applicationInsightsClient?: IApplicationInsightsClient;
    editorId?: string;
}

export interface IWorkspaceState
{
    workspace: IWorkspace;
    sequenceNumber: number;
    useWasmRunner: boolean;
}

export interface IWorkspace {
    workspaceType: string;
    language?: string;
    files?: IWorkspaceFile[];
    buffers: IWorkspaceBuffer[];
    usings?: string[];
    includeInstrumentation?: boolean;
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

export interface IWorkspaceInfo {
    originType: string;
}

export interface IGistWorkpaceInfo extends IWorkspaceInfo {
    htmlUrl: string;
    rawFileUrls: IRawFileUrl[];
}

export interface IRawFileUrl {
    fileName: string;
    url: string;
}
