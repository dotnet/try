// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { setBufferContent, cloneWorkspace } from "../workspaces";
import { IWorkspaceState, IWorkspace } from "../IState";

const initialState: IWorkspaceState = {
    workspace :
    {
        workspaceType: "script",
        files: [],
        buffers: [{ id: "Program.cs", content: "", position: 0 }],
        usings: []
    },
    sequenceNumber: 0,
    useWasmRunner: false,
};

export default function workspaceReducer(state: IWorkspaceState = initialState, action: Action) : IWorkspaceState {
    if (!action) {
        return state;
    }
    switch (action.type) {
        case types.CONFIGURE_WASMRUNNER:
            return {
                ...state,
                useWasmRunner: true
            };
        case types.LOAD_CODE_SUCCESS:
            return setWorkspaceBuffer(state, action.sourceCode, action.bufferId);
        case types.UPDATE_WORKSPACE_BUFFER:
            return setWorkspaceInstrumentation(
                setWorkspaceBuffer(state, action.content, action.bufferId),
                false
            );
        case types.SET_WORKSPACE:
            return {workspace: preserveOriginalBlazorWorkspaceType(state, cloneWorkspace(action.workspace)), sequenceNumber: state.sequenceNumber, useWasmRunner: state.useWasmRunner};
        case types.SET_ADDITIONAL_USINGS:
            return setWorkspaceUsings(state, action.additionalUsings);
        case types.SET_INSTRUMENTATION:
            return setWorkspaceInstrumentation(state, action.enabled);
        case types.SET_WORKSPACE_TYPE:
            return {
                workspace : {
                    ...state.workspace,
                    workspaceType: action.workspaceType
                },
                sequenceNumber: state.sequenceNumber + 1,
                useWasmRunner: state.useWasmRunner
            };
        default:
            return state;
    }
}

function preserveOriginalBlazorWorkspaceType(originalState: IWorkspaceState, newWorkspace: IWorkspace) : IWorkspace
{
    if (originalState.useWasmRunner)
    {
        newWorkspace.workspaceType = originalState.workspace.workspaceType;
    }
    
    return newWorkspace;
}

function setWorkspaceBuffer(state: IWorkspaceState, codeFragment: string, bufferId: string) : IWorkspaceState {
    const ret = cloneWorkspace(state.workspace);
    setBufferContent(ret, bufferId, codeFragment);
    return {workspace: ret, sequenceNumber: state.sequenceNumber + 1, useWasmRunner: state.useWasmRunner};
}

function setWorkspaceUsings(state: IWorkspaceState, additionalUsings: string[]): IWorkspaceState {
    const ret = cloneWorkspace(state.workspace);
    ret.usings = [...additionalUsings];
    return {workspace: ret, sequenceNumber: state.sequenceNumber + 1, useWasmRunner: state.useWasmRunner};
}

function setWorkspaceInstrumentation(state: IWorkspaceState, enabled: boolean): IWorkspaceState {
    const ret = cloneWorkspace(state.workspace);
    ret.includeInstrumentation = enabled;
    return {workspace: ret, sequenceNumber: state.sequenceNumber + 1, useWasmRunner: state.useWasmRunner};
}
