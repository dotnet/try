// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monacoEditor from "monaco-editor";
import IMlsClient from "../IMlsClient";
import { ICodeEditorForTryDotNet } from "./ICodeEditorForTryDotNet";
import {
    IDiagnostic,
    IInstrumentation,
    IVariableLocation,
    IWorkspace,
    IWorkspaceInfo
} from "../IState";
import { IHostConfiguration } from "./IHostConfiguration";
import { IApplicationInsightsClient } from "../ApplicationInsights";
import { Project, SourceFileRegion } from "../clientApiProtocol";

export const COMPILE_CODE_REQUEST = "COMPILE_CODE_REQUEST";
export const COMPILE_CODE_SUCCESS = "COMPILE_CODE_SUCCESS";
export const COMPILE_CODE_FAILURE = "COMPILE_CODE_FAILURE";

export const LOAD_CODE_REQUEST = "LOAD_CODE_REQUEST";
export const LOAD_CODE_SUCCESS = "LOAD_CODE_SUCCESS";
export const LOAD_CODE_FAILURE = "LOAD_CODE_FAILURE";
export const RUN_CODE_REQUEST = "RUN_CODE_REQUEST";
export const RUN_CODE_SUCCESS = "RUN_CODE_SUCCESS";
export const RUN_CODE_FAILURE = "RUN_CODE_FAILURE";
export const RUN_CODE_RESULT_SPECIFIED = "RUN_CODE_RESULT_SPECIFIED";
export const SET_WORKSPACE_TYPE = "SET_WORKSPACE_TYPE";
export const SET_ADDITIONAL_USINGS = "SET_ADDITIONAL_USINGS";
export const RUN_CLICKED = "RUN_CLICKED";
export const CONFIGURE_WASMRUNNER = "CONFIGURE_WASMRUNNER";
export const CONFIGURE_CLIENT = "CONFIGURE_CLIENT";
export const CONFIGURE_EDITOR_ID = "CONFIGURE_EDITOR_ID";
export const ENABLE_TELEMETRY = "ENABLE_TELEMETRY";
export const CONFIGURE_CODE_SOURCE = "CONFIGURE_CODE_SOURCE";
export const CONFIGURE_COMPLETION_PROVIDER = "CONFIGURE_COMPLETION_PROVIDER";
export const CONFIGURE_ENABLE_PREVIEW = "CONFIGURE_ENABLE_PREVIEW";
export const CONFIGURE_ENABLE_INSTRUMENTATION = "CONFIGURE_ENABLE_INSTRUMENTATION";
export const CONFIGURE_MONACO_EDITOR = "CONFIGURE_MONACO_EDITOR";
export const CONFIGURE_VERSION = "CONFIGURE_VERSION";
export const DEFINE_MONACO_EDITOR_THEMES = "DEFINE_MONACO_EDITOR_THEMES";
export const NOTIFY_HOST_PROVIDED_CONFIGURATION = "NOTIFY_HOST_PROVIDED_CONFIGURATION";
export const NOTIFY_HOST_LISTENER_READY = "NOTIFY_HOST_LISTENER_READY";
export const NOTIFY_HOST_EDITOR_READY = "NOTIFY_HOST_EDITOR_READY";
export const NOTIFY_HOST_RUN_READY = "NOTIFY_HOST_RUN_READY";
export const NOTIFY_MONACO_READY = "NOTIFY_MONACO_READY";
export const SHOW_EDITOR = "SHOW_EDITOR";
export const HIDE_EDITOR = "HIDE_EDITOR";
export const SERVICE_ERROR = "SERVICE_ERROR";
export const SET_WORKSPACE = "SET_WORKSPACE";
export const UPDATE_WORKSPACE_BUFFER = "UPDATE_WORKSPACE_BUFFER";
export const REPORT_ERROR = "REPORT_ERROR";
export const SET_ACTIVE_BUFFER = "SET_ACTIVE_BUFFER";
export const SET_WORKSPACE_INFO = "SET_WORKSPACE_INFO";
export const CAN_SHOW_GITHUB_PANEL = "CAN_SHOW_GITHUB_PANEL";
export const SET_INSTRUMENTATION = "SET_INSTRUMENTATION";
export const NEXT_INSTRUMENT_STEP = "NEXT_INSTRUMENT_STEP";
export const PREV_INSTRUMENT_STEP = "PREV_INSTRUMENT_STEP";
export const OUTPUT_UPDATED = "OUTPUT_UPDATED";
export const CANNOT_MOVE_NEXT = "CANNOT_MOVE_NEXT";
export const CANNOT_MOVE_PREV = "CANNOT_MOVE_PREV";
export const CAN_MOVE_NEXT = "CAN_MOVE_NEXT";
export const CAN_MOVE_PREV = "CAN_MOVE_PREV";

export const SEND_WASMRUNNER_MESSAGE = "SEND_WASMRUNNER_MESSAGE";
export const WASMRUNNER_READY = "WASMRUNNER_READY";

export const OPERATION_ID_GENERATED = "OPERATION_ID_GENERATED";

// client api types
export const CREATE_PROJECT_FAILURE = "CREATE_PROJECT_FAILURE";
export const CREATE_PROJECT_SUCCESS = "CREATE_PROJECT_SUCCESS";
export const CREATE_REGIONS_FROM_SOURCEFILES_FAILURE = "CREATE_REGIONS_FROM_SOURCEFILES_FAILURE";
export const CREATE_REGIONS_FROM_SOURCEFILES_SUCCESS = "GENERATE_REGIONS_FROM_SOURCEFILES_SUCCESS";

export const SET_DIAGNOSTICS = "SET_DIAGNOSTICS";
export const CONFIGURE_BRANDING = "CONFIGURE_BRANDING";

export type Action =
    {
        type: typeof CREATE_REGIONS_FROM_SOURCEFILES_SUCCESS,
        requestId: string,
        regions: SourceFileRegion[]
    } | {
        type: typeof CREATE_REGIONS_FROM_SOURCEFILES_FAILURE,
        requestId: string,
        error: Error
    } | {
        type: typeof CREATE_PROJECT_SUCCESS,
        requestId: string,
        project: Project
    } | {
        type: typeof CREATE_PROJECT_FAILURE,
        requestId: string,
        error: Error
    } | {
        type: typeof OPERATION_ID_GENERATED,
        operationId: string,
        requestId: string
    } | {
        type: typeof SEND_WASMRUNNER_MESSAGE,
        payload: object,
        callback: (arg: any) => void
    } | {
        type: typeof WASMRUNNER_READY,
        editorId?: string
    } | {
        type: typeof LOAD_CODE_REQUEST,
        from: string
    } | {
        type: typeof LOAD_CODE_SUCCESS,
        sourceCode: string,
        bufferId?: string
    } | {
        type: typeof LOAD_CODE_FAILURE,
        ex: Error
    } | {
        type: typeof SET_WORKSPACE_TYPE,
        workspaceType: string
    } | {
        type: typeof SET_ADDITIONAL_USINGS,
        additionalUsings: string[]
    } | {
        type: typeof CONFIGURE_CLIENT,
        client: IMlsClient
    } | {
        type: typeof CONFIGURE_EDITOR_ID,
        editorId: string
    } | {
        type: typeof CONFIGURE_CODE_SOURCE,
        from: string,
        sourceCode: string
    } | {
        type: typeof CONFIGURE_COMPLETION_PROVIDER,
        completionProvider: string
    } | {
        type: typeof CONFIGURE_VERSION,
        version: number
    } | {
        type: typeof NOTIFY_HOST_PROVIDED_CONFIGURATION,
        configuration: IHostConfiguration
    } | {
        type: typeof HIDE_EDITOR
    } | {
        type: typeof NOTIFY_HOST_LISTENER_READY,
        editorId?: string
    } | {
        type: typeof NOTIFY_HOST_EDITOR_READY,
        editorId?: string
    } | {
        type: typeof NOTIFY_HOST_RUN_READY,
        editorId?: string
    } | {
        type: typeof NOTIFY_MONACO_READY,
        editor: ICodeEditorForTryDotNet
    } | {
        type: typeof RUN_CODE_SUCCESS,
        exception?: string,
        output: string[],
        succeeded: boolean,
        diagnostics?: IDiagnostic[],
        instrumentation?: IInstrumentation[],
        variableLocations?: IVariableLocation[],
        requestId?: string,
        executionStrategy: "Agent" | "Blazor"
    } | {
        type: typeof RUN_CODE_FAILURE,
        requestId: string,
        ex: Error
    } | {
        type: typeof RUN_CODE_REQUEST,
        requestId: string
    } | {
        type: typeof CONFIGURE_MONACO_EDITOR,
        editorOptions: monacoEditor.editor.IEditorOptions
        theme: string
    } | {
        type: typeof RUN_CODE_RESULT_SPECIFIED,
        exception?: string,
        output: string[],
        succeeded: boolean,
        diagnostics?: IDiagnostic[],
        instrumentation?: IInstrumentation[],
        variableLocations?: IVariableLocation[],
        requestId?: string
    } | {
        type: typeof SHOW_EDITOR
    } | {
        type: typeof DEFINE_MONACO_EDITOR_THEMES,
        themes: { [x: string]: monacoEditor.editor.IStandaloneThemeData }
    } | {
        type: typeof SET_WORKSPACE,
        workspace: IWorkspace
    } | {
        type: typeof REPORT_ERROR,
        errorType: string,
        reason?: string
    } | {
        type: typeof UPDATE_WORKSPACE_BUFFER,
        content: string,
        bufferId: string
    } | {
        type: typeof SET_ACTIVE_BUFFER,
        bufferId: string
    } | {
        type: typeof SET_WORKSPACE_INFO,
        workspaceInfo: IWorkspaceInfo
    } | {
        type: typeof CAN_SHOW_GITHUB_PANEL,
        canShow: boolean
    } | {
        type: typeof SET_INSTRUMENTATION,
        enabled: boolean
    } | {
        type: typeof NEXT_INSTRUMENT_STEP
    } | {
        type: typeof PREV_INSTRUMENT_STEP
    } | {
        type: typeof OUTPUT_UPDATED,
        output: string[]
    } | {
        type: typeof CANNOT_MOVE_NEXT
    } | {
        type: typeof CANNOT_MOVE_PREV
    } | {
        type: typeof CAN_MOVE_NEXT
    } | {
        type: typeof CAN_MOVE_PREV
    } | {
        type: typeof CONFIGURE_ENABLE_PREVIEW
    } | {
        type: typeof CONFIGURE_WASMRUNNER
    } | {
        type: typeof COMPILE_CODE_FAILURE,
        requestId?: string,
        diagnostics: IDiagnostic[],
        workspaceVersion: number
    } | {
        type: typeof COMPILE_CODE_REQUEST,
        requestId: string,
        workspaceVersion: number
    } | {
        type: typeof COMPILE_CODE_SUCCESS,
        requestId?: string,
        base64assembly: string,
        workspaceVersion: number
    } | {
        type: typeof CONFIGURE_ENABLE_INSTRUMENTATION
    } | {
        type: typeof ENABLE_TELEMETRY,
        client: IApplicationInsightsClient
    } | {
        type: typeof SET_DIAGNOSTICS,
        diagnostics: IDiagnostic[]
    } | {
        type: typeof CONFIGURE_BRANDING
        visible: boolean
    };

