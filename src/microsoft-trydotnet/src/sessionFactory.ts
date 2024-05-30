// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ISession } from "./session";
import { Configuration } from "./configuration";
import { Session, InitialSessionState, Document, DocumentObject } from "./internals/session";
import { IFrameMessageBus } from "./internals/messageBus";
import { configureEmbeddableEditorIFrame, configureEmbeddableEditorIFrameWithPackage } from "./htmlDomHelpers";
import { Project } from "./project";
import { Logger } from "@microsoft/polyglot-notebooks";
import { configureLogging } from "./log";
import { IMonacoEditor } from "./editor";

async function _createSession(configuration: Configuration, editorIFrame: HTMLIFrameElement, window: Window, initialState: InitialSessionState, configureEmbeddableEditorIFrameCallBack: (editorIFrame: HTMLIFrameElement, configuration: Configuration) => void): Promise<ISession> {


  configureLogging({ enableLogging: configuration?.enableLogging === true });

  let messageBus = new IFrameMessageBus(editorIFrame, window);
  let session = new Session(messageBus);

  // listen for size and visibility changes
  try {
    const resizeObserver = new ResizeObserver((entries, _observer) => {
      for (const entry of entries) {
        const { width, height } = entry.contentRect;

        const computedStyle = window.getComputedStyle(editorIFrame);

        const paddingLeft = parseFloat(computedStyle.paddingLeft) || 5;
        const paddingRight = parseFloat(computedStyle.paddingRight) || 5;
        const paddingTop = parseFloat(computedStyle.paddingTop) || 5;
        const paddingBottom = parseFloat(computedStyle.paddingBottom) || 5;

        const borderLeft = parseFloat(computedStyle.borderLeftWidth) || 0;
        const borderRight = parseFloat(computedStyle.borderRightWidth) || 0;
        const borderTop = parseFloat(computedStyle.borderTopWidth) || 0;
        const borderBottom = parseFloat(computedStyle.borderBottomWidth) || 0;

        const adjustedWidth = width - paddingLeft - paddingRight - borderLeft - borderRight;
        const adjustedHeight = height - paddingTop - paddingBottom - borderTop - borderBottom;

        let editor = <IMonacoEditor>(session.getTextEditor());
        editor.setSize({ width: adjustedWidth, height: adjustedHeight });
      }
    });

    resizeObserver.observe(editorIFrame);
  } catch (e) {
    Logger.default.error("Error creating ResizeObserver");
  }

  editorIFrame.addEventListener("load", () => {
    configureEmbeddableEditorIFrameCallBack(editorIFrame, configuration);
  });

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
