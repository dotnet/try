// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ISession } from "./session";
import { Configuration } from "./configuration";
import { Session, InitialSessionState, Document, DocumentObject } from "./internals/session";
import { IFrameMessageBus } from "./internals/messageBus";
import { configureEmbeddableEditorIFrame, configureEmbeddableEditorIFrameWithPackage } from "./htmlDomHelpers";
import { Project } from "./project";
import { Logger, LogLevel } from "@microsoft/dotnet-interactive";
import { configureLogging } from "./log";

async function _createSession(configuration: Configuration, editorIFrame: HTMLIFrameElement, window: Window, initialState: InitialSessionState, configureEmbeddableEditorIFrameCallBack: (editorIFrame: HTMLIFrameElement, configuration: Configuration) => void): Promise<ISession> {


  configureLogging({ enableLogging: configuration?.enableLogging === true });

  let messageBus = new IFrameMessageBus(editorIFrame, window);
  let session = new Session(messageBus);

  let src = editorIFrame.getAttribute("src");
  if (!src) {
    configureEmbeddableEditorIFrameCallBack(messageBus.targetIframe(), configuration);
  }

  Logger.default.info("----  start createSession");
  await session.configureAndInitialize(configuration, initialState);
  Logger.default.info("----  end createSession");
  session.enableLogging(configuration?.enableLogging === true);
  return session;
}

export function createSession(configuration: Configuration, editorIFrame: HTMLIFrameElement[] | HTMLIFrameElement, window: Window): Promise<ISession> {
  return _createSession(configuration, getIframe(editorIFrame), window, undefined, (editorIFrame, configuration) => {
    configureEmbeddableEditorIFrame(editorIFrame, configuration);
  });
}

export async function createSessionWithProject(configuration: Configuration, editorIFrame: HTMLIFrameElement[] | HTMLIFrameElement, window: Window, project: Project, documentsToInclude?: DocumentObject[]): Promise<ISession> {
  return _createSession(configuration, getIframe(editorIFrame), window, { project: project, documentsToInclude: documentsToInclude }, (editorIFrame, configuration) => {
    configureEmbeddableEditorIFrameWithPackage(editorIFrame, configuration, project.package);
  });
}

export async function createSessionWithProjectAndOpenDocument(configuration: Configuration, editorIFrame: HTMLIFrameElement[] | HTMLIFrameElement, window: Window, project: Project, document: Document, documentsToInclude?: DocumentObject[]): Promise<ISession> {
  return _createSession(configuration, getIframe(editorIFrame), window, { project: project, openDocument: document, documentsToInclude: documentsToInclude }, (editorIFrame, configuration) => {
    configureEmbeddableEditorIFrameWithPackage(editorIFrame, configuration, project.package);
  });
}

function getIframe(editorIFrame: HTMLIFrameElement[] | HTMLIFrameElement): HTMLIFrameElement {
  if (editorIFrame instanceof Array) {
    return editorIFrame[0];
  } else {
    return editorIFrame;
  }
}
