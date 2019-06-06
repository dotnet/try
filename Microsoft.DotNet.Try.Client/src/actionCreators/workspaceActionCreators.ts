// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import { Action } from "../constants/ActionTypes";
import { ActionCreator } from "redux";
import IState, { IWorkspace, IWorkspaceInfo, IWorkspaceFile, IWorkspaceBuffer } from "../IState";
import { decodeWorkspace } from "../workspaces";
import { error } from "./errorActionCreators";
import { setCodeSource } from "./configActionCreators";
import { loadSource } from "./sourceCodeActionCreators";
import * as uiActions from "./uiActionCreators";
import { IStore } from "../IStore";
import { ThunkDispatch, ThunkAction } from "redux-thunk";
import { string } from "prop-types";
import { isNullOrUndefinedOrWhitespace } from "../utilities/stringUtilities";

export function setWorkspaceInfo(workspaceInfo: IWorkspaceInfo): Action {
    return {
        type: types.SET_WORKSPACE_INFO,
        workspaceInfo
    };
}

export function setWorkspace(workspace: IWorkspace): Action {
    return {
        type: types.SET_WORKSPACE,
        workspace
    };
}

export const setWorkspaceAndActiveBuffer: ActionCreator<ThunkAction<Promise<Action>, IState, void, Action>> =
    (workspace: IWorkspace, activeBufferId: string) =>
        async (dispatch: ThunkDispatch<IState, void, Action>): Promise<Action> => {
            dispatch(setWorkspace(workspace));
            return dispatch(setActiveBufferAndLoadCode(activeBufferId));
        };

export function setWorkspaceType(workspaceType: string): Action {
    return {
        type: types.SET_WORKSPACE_TYPE as typeof types.SET_WORKSPACE_TYPE,
        workspaceType: workspaceType
    };
}

export function updateWorkspaceBuffer(content: string, bufferId: string): Action {
    return {
        type: types.UPDATE_WORKSPACE_BUFFER,
        content,
        bufferId
    };
}

export function setActiveBuffer(bufferId: string): Action {
    return {
        type: types.SET_ACTIVE_BUFFER,
        bufferId
    };
}

export function setInstrumentation(enabled: boolean): Action {
    return {
        type: types.SET_INSTRUMENTATION,
        enabled: enabled
    };
}

export const setActiveBufferAndLoadCode: ActionCreator<ThunkAction<Promise<Action>, IState, void, Action>> =
    (bufferId: string) =>
        async (dispatch: ThunkDispatch<IState, void, Action>): Promise<Action> => {
            dispatch(setActiveBuffer(bufferId));
            dispatch(setCodeSource("workspace"));
            return dispatch(loadSource());
        };

export const updateCurrentWorkspaceBuffer: ActionCreator<ThunkAction<Action, IState, void, Action>> =
    (content: string) =>
        (dispatch: ThunkDispatch<IState, void, Action>, getState: () => IState): Action => {
            const state = getState();
            const bufferId = state.monaco.bufferId;
            return dispatch(updateWorkspaceBuffer(content, bufferId));
        };

export const LoadWorkspaceFromGist: ActionCreator<ThunkAction<Promise<Action>, IState, void, Action>> =
    (gistId: string, bufferId: string = null, workspaceType: string, canShowGitHubPanel: boolean = false) =>
        async (dispatch: ThunkDispatch<IState, void, Action>, getState: () => IState): Promise<Action> => {
            const state = getState();
            const client = state.config.client;
            let activeBufferId = bufferId;
            let extractBuffers = false;
            if (activeBufferId) {
                extractBuffers = activeBufferId.indexOf("@") >= 0;
            }
            const workspaceInfo = await client.getWorkspaceFromGist(gistId, workspaceType, extractBuffers);
            if (!activeBufferId) {
                activeBufferId = workspaceInfo.workspace.buffers[0].id;
            }
            dispatch(setWorkspaceInfo(workspaceInfo));
            if (canShowGitHubPanel) {
                dispatch(uiActions.canShowGitHubPanel(true));
            }
            else {
                dispatch(uiActions.canShowGitHubPanel(false));
            }

            return dispatch(setWorkspaceAndActiveBuffer(workspaceInfo.workspace, activeBufferId));
        };

export function configureWorkspace(configuration: { store: IStore, workspaceParameter?: string, workspaceTypeParameter?: string, language?: string, fromParameter?: string, bufferIdParameter?: string, fromGistParameter?: string, canShowGitHubPanelQueryParameter?: string }) {
    let bufferId = "Program.cs";
    if (configuration.bufferIdParameter) {
        bufferId = decodeURIComponent(configuration.bufferIdParameter);
    }

    let LoadFromWorkspace = false;
    let workspace: IWorkspace = {
        workspaceType: "script",
        files: [],
        buffers: [{ id: bufferId, content: "", position: 0 }],
        usings: []
    };

    if (!isNullOrUndefinedOrWhitespace(configuration.language)) {
        workspace.language = configuration.language;
    }

    if (configuration.workspaceParameter) {
        if (configuration.fromParameter) {
            configuration.store.dispatch(error("parameter loading", "cannot define `workspace` and `from` simultaneously"));
        }
        if (configuration.workspaceTypeParameter) {
            configuration.store.dispatch(error("parameter loading", "cannot define `workspace` and `workspaceTypeParameter` simultaneously"));
        }
        LoadFromWorkspace = true;
        workspace = decodeWorkspace(configuration.workspaceParameter);

    } else {
        if (configuration.workspaceTypeParameter) {
            workspace.workspaceType = decodeURIComponent(configuration.workspaceTypeParameter);
        }
    }

    configuration.store.dispatch(setWorkspaceType(workspace.workspaceType));
    configuration.store.dispatch(setWorkspace(workspace));
    configuration.store.dispatch(setActiveBuffer(bufferId));

    if (LoadFromWorkspace) {
        configuration.store.dispatch(setCodeSource("workspace"));
    }
    else if (configuration.fromGistParameter) {
        if (configuration.canShowGitHubPanelQueryParameter) {
            let canShowGitHubPanel = decodeURIComponent(configuration.canShowGitHubPanelQueryParameter) === "true";
            if (canShowGitHubPanel) {
                configuration.store.dispatch(uiActions.canShowGitHubPanel(true));
            }
            else {
                configuration.store.dispatch(uiActions.canShowGitHubPanel(false));
            }
        }
        const fromGist = `gist::${decodeURIComponent(configuration.fromGistParameter)}`;
        configuration.store.dispatch(setCodeSource(fromGist));
    }
    else if (configuration.fromParameter) {
        const from = decodeURIComponent(configuration.fromParameter);
        configuration.store.dispatch(setCodeSource(from));
    }
}

export type Scaffolding =
    "None" | "Class" | "Method";


function getContent(type: Scaffolding, additionalUsings: string[]): string {
    let usings = "";
    additionalUsings.forEach(using => {
        usings += `using ${using};
`});

    switch (type) {
        case "Class":
            return `${usings}class C
{
#region scaffold               
#endregion                  
}`;

        case "Method":
            return `${usings}class C
{
public static void Main()
    {
#region scaffold               
#endregion
    }                
}`;

        case "None":
            return "";
        default:
            throw "Invalid scaffolding type";
    }
}

function getActiveBufferId(type: Scaffolding, fileName: string): string {
    switch (type) {
        case "Class":
        case "Method":
            return `${fileName}@scaffold`;
        case "None":
            return fileName;
        default:
            throw "Invalid scaffolding type";
    }
}

export const applyScaffolding: ActionCreator<ThunkAction<Promise<Action>, IState, void, Action>> =
    (type: Scaffolding, fileName: string, additionalUsings: string[] = []) =>
        (dispatch: ThunkDispatch<IState, void, Action>, getState: () => IState): Promise<Action> => {
            if (type === null || !fileName) {
                dispatch(error("general", "invalid scaffolding parameter"))
            }

            let workspaceType = getState().workspace.workspace.workspaceType;

            let file: IWorkspaceFile = {
                name: fileName,
                text: getContent(type, additionalUsings)
            };

            let activeBufferId = getActiveBufferId(type, fileName);
            let buffer: IWorkspaceBuffer = {
                content: "",
                id: activeBufferId,
                position: 0
            };

            let workspace: IWorkspace = {
                activeBufferId: activeBufferId,
                buffers: [buffer],
                files: [file],
                workspaceType,
            };

            dispatch(setWorkspace(workspace));
            dispatch(setActiveBuffer(activeBufferId));
            dispatch(setCodeSource("workspace"));
            return dispatch(loadSource());

        };



export function getProjectTemplateFromStore(store: IStore): string {
    let template = "unspecified";
    if (store) {
        let state = store.getState();
        template = getProjectTemplateFromState(state);
    }
    return template;
}

export function getProjectTemplateFromState(state: IState): string {
    let template = "unspecified";

    if (state && state.workspace && state.workspace.workspace && state.workspace.workspace.workspaceType) {
        template = state.workspace.workspace.workspaceType;
    }

    return template;
}
