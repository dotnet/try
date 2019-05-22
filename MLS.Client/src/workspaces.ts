// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IWorkspace, IWorkspaceBuffer, IWorkspaceFile } from "./IState";
require("jsdom-global")();

export function encodeWorkspace(workspace: IWorkspace): string {
    return encodeURIComponent(btoa(JSON.stringify(workspace)));
}

export function decodeWorkspace(encodedWorkspace: string): IWorkspace {
    return JSON.parse(atob(decodeURIComponent(encodedWorkspace))) as IWorkspace;
}

export function getBufferContent(workspace: IWorkspace, bufferId: string): string {
    let idx = workspace.buffers.findIndex(b => b.id === bufferId);
    let content = "";
    if(idx >= 0){
        content = workspace.buffers[idx].content;
    }
    return content;
}

export function setBufferContent(workspace: IWorkspace, bufferId: string, content: string) {
    let idx = workspace.buffers.findIndex(b => b.id === bufferId);
    if(idx >= 0){
        workspace.buffers[idx].content = content;
    }
}

export function cloneWorkspace(source: IWorkspace): IWorkspace {
    let ret = {...source};
    if(source.buffers)
    {
        ret.buffers = cloneWorkspaceBuffers(source);
    }else{
        ret.buffers = [] as IWorkspaceBuffer[];
    }
    if(source.files)
    {
        ret.files = cloneWorkspaceFiles(source);
    }else{
        ret.files = [] as IWorkspaceFile[];
    }

    if(source.usings){
        ret.usings = [...source.usings];
    }
    
    return ret;
}

export function cloneWorkspaceBuffers(source: IWorkspace): IWorkspaceBuffer[]{
    let ret = source.buffers.map(b => ({...b}));
    return ret;
}

export function cloneWorkspaceFiles(source: IWorkspace): IWorkspaceFile[]{
    let ret = source.files.map(f => ({...f}));
    return ret;
}
