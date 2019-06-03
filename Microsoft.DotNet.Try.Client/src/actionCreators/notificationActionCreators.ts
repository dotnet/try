// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";

import { Action } from "../constants/ActionTypes";
import { IHostConfiguration } from "../constants/IHostConfiguration";
import { ICodeEditorForTryDotNet } from "../constants/ICodeEditorForTryDotNet";

export function notifyHostProvidedConfiguration(configuration: IHostConfiguration) {
    return {
        type: types.NOTIFY_HOST_PROVIDED_CONFIGURATION,
        configuration
    };
}

export function hostListenerReady(editorId?: string): Action {
    let action: Action = {
        type: types.NOTIFY_HOST_LISTENER_READY
    };

    if (editorId) {
        action.editorId = editorId;
    }
    return action;
}

export function hostEditorReady(editorId?: string): Action {
    let action: Action = {
        type: types.NOTIFY_HOST_EDITOR_READY
    };

    if (editorId) {
        action.editorId = editorId;
    }
    return action;
}

export function hostRunReady(editorId?: string): Action {
    let action: Action = {
        type: types.NOTIFY_HOST_RUN_READY
    };

    if (editorId) {
        action.editorId = editorId;
    }
    return action;
}

export function notifyMonacoReady(editor: ICodeEditorForTryDotNet): Action {
    return {
        type: types.NOTIFY_MONACO_READY,
        editor: editor
    };
}
