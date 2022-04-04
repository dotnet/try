// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IOutputPanel } from "../outputPanel";
import { ISession, RunResult, ServiceError } from "../session";

export type SessionLookup = {
    [key: string]: {
        codeSources: HTMLElement[];
        runButton?: HTMLButtonElement;
        outputPanel?: HTMLDivElement;
        errorPanel?: HTMLDivElement;
    };
};

export type RunResultHandler = (
    runResult: RunResult,
    container: HTMLElement,
    sessionId: string
) => void;

export type ServiceErrorHandler = (
    error: ServiceError,
    container: HTMLElement,
    serviceErrorDiv: IOutputPanel,
    sessionId: string
) => void;

export type AutoEnablerConfiguration = {
    apiBaseAddress: URL,
    useWasmRunner?:boolean;
    debug?:boolean;
    runResultHandler?: RunResultHandler;
    serviceErrorHandler?: ServiceErrorHandler;
    editorConfiguration?: { [key: string]: any };
};

export enum tryDotNetModes {
    editor = "editor",
    run = "run",
    runResult = "runResult",
    errorReport = "errorReport",
    include = "include",
    settings = "settings"
}

export enum tryDotNetRegionInjectionPoints {
    before = "before",
    after = "after",
    replace = "replace"
}

export enum tryDotNetVisibilityModifiers {
   visible= "visible",
   hidden= "hidden",
}

export enum tryDotNetOutputModes {
    standard = "standard",
    terminal = "terminal"
}

export type TryDotNetSession = {
    sessionId: string;
    session: ISession;
    outputPanels?: IOutputPanel[];
    runButtons?: HTMLElement[];
    editorIframes?: HTMLIFrameElement[];
};
