// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface IWorkspace {
  workspaceType: string;
  language?: string;
  files?: IWorkspaceFile[];
  buffers: IWorkspaceBuffer[];
  usings?: string[];
  includeInstrumentation?: boolean;
  activeBufferId?: string;
}

export interface IWorkspaceState {
  workspace: IWorkspace;
  sequenceNumber: number;
  useWasmRunner: boolean;
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
export function encodeWorkspace(workspace: IWorkspace): string {
  let buffer = Buffer.from(JSON.stringify(workspace));
  return encodeURIComponent(buffer.toString("base64"));
}

export function decodeWorkspace(encodedWorkspace: string): IWorkspace {
  let data = Buffer.from(decodeURIComponent(encodedWorkspace), "base64");
  return JSON.parse(data.toString()) as IWorkspace;
}

export function getBufferContent(workspace: IWorkspace, bufferId: string): string {
  const idx = workspace.buffers.findIndex(b => b.id === bufferId);
  let content = '';
  if (idx >= 0) {
    content = workspace.buffers[idx].content;
  }
  return content;
}

export function setBufferContent(workspace: IWorkspace, bufferId: string, content: string) {
  const idx = workspace.buffers.findIndex(b => b.id === bufferId);
  if (idx >= 0) {
    workspace.buffers[idx].content = content;
  }
}

export function cloneWorkspace(source: IWorkspace): IWorkspace {
  const ret = { ...source };
  if (source.buffers) {
    ret.buffers = cloneWorkspaceBuffers(source);
  } else {
    ret.buffers = [] as IWorkspaceBuffer[];
  }
  if (source.files) {
    ret.files = cloneWorkspaceFiles(source);
  } else {
    ret.files = [] as IWorkspaceFile[];
  }

  if (source.usings) {
    ret.usings = [...source.usings];
  }

  return ret;
}

export function cloneWorkspaceBuffers(source: IWorkspace): IWorkspaceBuffer[] {
  const ret = source.buffers.map(b => ({ ...b }));
  return ret;
}

export function cloneWorkspaceFiles(source: IWorkspace): IWorkspaceFile[] {
  const ret = source.files.map(f => ({ ...f }));
  return ret;
}
