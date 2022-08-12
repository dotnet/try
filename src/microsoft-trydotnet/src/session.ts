// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from "./diagnostics";
import { Project } from "./project";
import { Region, IDocument } from "./editableDocument";
import { ITextEditor } from "./editor";
import { Unsubscribable } from "rxjs";


export type RunResult = {
    runId: string
    succeeded: boolean;
    diagnostics?: Diagnostic[];
    output?: string[];
    exception?: any;
};

export type ServiceError = {
    statusCode: string,
    message: string,
    requestId: string,
};

export type CompilationResult = {
    succeeded: boolean;
    diagnostics?: Diagnostic[];
};

export type RunConfiguration = {
    instrument?: boolean;
    runWorkflowId?: string;
    runArgs?: string;
};

export type OutputEvent = {
    stdout?: string[];
    exception?: any;
}

export interface OutputEventSubscriber {
    (event: OutputEvent): void;
}

export interface ServiceErrorSubscriber {
    (error: ServiceError): void;
}

export type OpenDocumentParameters = {
    fileName: string,
    region?: Region,
    content?: string
}
export interface ISession {
    openProject(project: Project): Promise<void>;
    openDocument(parameters: OpenDocumentParameters): Promise<IDocument>;
    getOpenDocument(): IDocument;

    getTextEditor(): ITextEditor;

    run(configuration?: RunConfiguration): Promise<RunResult>;

    subscribeToOutputEvents(handler: OutputEventSubscriber): Unsubscribable;
    subscribeToServiceErrorEvents(handler: ServiceErrorSubscriber): Unsubscribable;

    onCanRunChanged(changed: (canRun: boolean) => void): void;
    enableLogging(enableLogging: boolean): void;
}
