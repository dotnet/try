// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ISession } from "./session";
import { Configuration } from "./configuration";
import { Session, InitialSessionState, Document, DocumentsToOpen, DocumentObject } from "./internals/session";
import { IFrameMessageBus, tryGetEditorId } from "./internals/messageBus";
import { configureEmbeddableEditorIFrame, configureEmbeddableEditorIFrameWithPackage } from "./htmlDomHelpers";
import { Project } from "./project";
import { Region } from "./editableDocument";

async function _createSession(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: Window, initialState: InitialSessionState, configureEmbeddableEditorIFrameCallBack: (editorIFrame: HTMLIFrameElement, messageBusId: string, configuration: Configuration) => void): Promise<ISession> {
  let messageBuses = editorIFrames.map((editorIFrame, index) => new IFrameMessageBus(editorIFrame, window, tryGetEditorId(editorIFrame, index.toString())));
  let session = new Session(messageBuses);

  for (let messageBus of messageBuses) {
    let editorIFrame = messageBus.targetIframe();
    let src = editorIFrame.getAttribute("src");
    if (!src) {
      configureEmbeddableEditorIFrameCallBack(editorIFrame, messageBus.id(), configuration);
    }
  }

  await session.configureAndInitialize(configuration, initialState);
  return session;
}

export function createSession(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: Window): Promise<ISession> {
  return _createSession(configuration, editorIFrames, window, undefined, (editorIFrame, messageBusId, configuration) => {
    configureEmbeddableEditorIFrame(editorIFrame, messageBusId, configuration);
  });
}

export async function createSessionWithPackage(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: Window, packageName: string): Promise<ISession> {
  return _createSession(configuration, editorIFrames, window, undefined, (editorIFrame, messageBusId, configuration) => {
    configureEmbeddableEditorIFrameWithPackage(editorIFrame, messageBusId, configuration, packageName);
  });
}

export async function createSessionWithProject(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: Window, project: Project,documentsToInclude?: DocumentObject[]): Promise<ISession> {
  return _createSession(configuration, editorIFrames, window, { project: project, documentsToInclude: documentsToInclude }, (editorIFrame, messageBusId, configuration) => {
    configureEmbeddableEditorIFrameWithPackage(editorIFrame, messageBusId, configuration, project.package);
  });
}

export async function createSessionWithProjectAndOpenDocument(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: Window, project: Project, document: Document,documentsToInclude?: DocumentObject[]): Promise<ISession> {
  return _createSession(configuration, editorIFrames, window, { project: project, openDocument: document, documentsToInclude: documentsToInclude }, (editorIFrame, messageBusId, configuration) => {
    configureEmbeddableEditorIFrameWithPackage(editorIFrame, messageBusId, configuration, project.package);
  });
}

export async function createSessionWithProjectAndOpenDocuments(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: Window, project: Project, documents: DocumentsToOpen, documentsToInclude?: DocumentObject[]): Promise<ISession> {
  return _createSession(configuration, editorIFrames, window, { project: project, openDocuments: documents, documentsToInclude: documentsToInclude }, (editorIFrame, messageBusId, configuration) => {
    configureEmbeddableEditorIFrameWithPackage(editorIFrame, messageBusId, configuration, project.package);
  });
}
