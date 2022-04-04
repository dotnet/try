// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, SourceFile, createProject } from "../src/index";
import { JSDOM } from "jsdom";
import { buildSimpleIFrameDom, getEditorIFrame, buildMultiIFrameDom, getEditorIFrames, buildDoubleIFrameDom } from "./domUtilities";
import * as chaiAsPromised from "chai-as-promised";
import { registerForRegionFromFile, registerForRequestIdGeneration, registerForEditorMessages, notifyEditorReady, EditorState, trackSetWorkspaceRequests, raiseTextChange } from "./messagingMocks";
import { wait } from "./wait";
import { createReadySession, createReadySessionWithMultipleEditors } from "./sessionFactory";
import { ApiMessage, SET_WORKSPACE_REQUEST } from "../src/internals/apiMessages";
import { IWorkspace } from "../src/internals/workspace";
import { expect } from "chai";

chai.use(chaiAsPromised);
chai.should();



describe("A user", () => {

    let configuration: Configuration;
    let dom: JSDOM;
    let editorIFrame: HTMLIFrameElement;

    beforeEach(() => {
        configuration = { hostOrigin: "https://docs.microsoft.com" };
        dom = buildSimpleIFrameDom(configuration);
        editorIFrame = getEditorIFrame(dom);
    });
    
    describe("with a trydotnet session", () => {
    });
});