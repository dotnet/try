// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import * as chaiAsPromised from "chai-as-promised";
import { Configuration, createSessionWithProjectAndOpenDocument, createSession } from "../src/index";
import { buildSimpleIFrameDom, getEditorIFrames, buildMultiIFrameDom } from "./domUtilities";
import { notifyEditorReady, notifyEditorReadyWithId } from "./messagingMocks";
import { tryGetEditorId } from "../src/internals/messageBus";

chai.use(chaiAsPromised);
chai.should();

describe("a user", () => {
    let configuration: Configuration;

    beforeEach(() => {
        configuration = { hostOrigin: "https://docs.microsoft.com" };
    });
    describe("with single iframe", () => {
        it("can create a session with initial project", async () => {
            let dom = buildSimpleIFrameDom(configuration);
            let editorIFrames = getEditorIFrames(dom);
            let awaitableSession = createSessionWithProjectAndOpenDocument(
                configuration,
                editorIFrames,
                dom.window,
                {
                    package: "console",
                    files: [{ name: "program.cs", content: "" }]
                },
                "program.cs");

            notifyEditorReady(configuration, dom.window);
            let session = await awaitableSession;
            session.should.not.be.null;
        });
    });
    describe("with multiple iframes", () => {
        it("can create a session", async () => {
            let dom = buildMultiIFrameDom(configuration);
            let editorIFrames = getEditorIFrames(dom);
            let awaitableSession = createSession(configuration,
                editorIFrames,
                dom.window);

            editorIFrames.forEach((iframe, index) => {
                notifyEditorReadyWithId(configuration, dom.window, tryGetEditorId(iframe, index.toString()));
            });

            let session = await awaitableSession;
            session.should.not.be.null;
        });

        it("requires all buses to be ready", async () => {
            let dom = buildMultiIFrameDom(configuration);
            let editorIFrames = getEditorIFrames(dom);
            let awaitableSession = createSession(configuration,
                editorIFrames,
                dom.window);

            notifyEditorReadyWithId(configuration, dom.window, tryGetEditorId(editorIFrames[0], "0"));

            awaitableSession.should.not.be.fulfilled;
        });
    });
});