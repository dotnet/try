// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";

import { Action } from "../constants/ActionTypes";
import { IHostConfiguration } from "../constants/IHostConfiguration";

import IMlsClient from "../IMlsClient";
import { IApplicationInsightsClient } from "../ApplicationInsights";

export function setClient(client: IMlsClient): Action {
    return {
        type: types.CONFIGURE_CLIENT as typeof types.CONFIGURE_CLIENT,
        client: client
    };
}

export function setCodeSource(from: string, sourceCode: string = undefined): Action {
    return {
        type: types.CONFIGURE_CODE_SOURCE as typeof types.CONFIGURE_CODE_SOURCE,
        from: from,
        sourceCode: sourceCode
    };
}

export function notifyHostProvidedConfiguration(configuration: IHostConfiguration): Action {
    return {
        type: types.NOTIFY_HOST_PROVIDED_CONFIGURATION,
        configuration
    };
}

export function setVersion(version: number): Action {
    return {
        type: types.CONFIGURE_VERSION,
        version
    };
}

export function hideEditor() {
    return {
        type: types.HIDE_EDITOR
    };
}

export function setCompletionProvider(completionProvider: string): Action { 
    return { 
        type: types.CONFIGURE_COMPLETION_PROVIDER, 
        completionProvider 
    }; 
} 

export function enablePreviewFeatures(): Action {
    return {
        type: types.CONFIGURE_ENABLE_PREVIEW
    };
}

export function configureWasmRunner(): Action {
    return {
        type: types.CONFIGURE_WASMRUNNER
    };
}
export function enableInstrumentation(): Action {
    return {
        type: types.CONFIGURE_ENABLE_INSTRUMENTATION
    };
}

export function enableClientTelemetry(client: IApplicationInsightsClient): Action {
    return {
        type: types.ENABLE_TELEMETRY,
        client: client
    };
}

export function configureEditorId(editorId: string): Action {
    return {
        type: types.CONFIGURE_EDITOR_ID,
        editorId: editorId
    };
}
