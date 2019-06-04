// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, createProject } from "../src/index";
import { buildSimpleIFrameDom, getEditorIFrame } from "./domUtilities";
import { JSDOM } from "jsdom";
import { createReadySession } from "./sessionFactory";
chai.should();

describe.skip("a user", () => {
    let configuration: Configuration;
    let dom: JSDOM;
    let editorIFrame: HTMLIFrameElement;

    beforeEach(() => {
        configuration = { hostOrigin: "https://docs.microsoft.com" };
        dom = buildSimpleIFrameDom(configuration);
        editorIFrame = getEditorIFrame(dom);
    });

    describe("with a trydotnet session", () => {
        it("can request completion list", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "Console.W" }] })
            session.openProject(project);
            let completionListResult = await session.getCompletionList("program.cs", 9);
            completionListResult.should.not.be.null;
        });

        it("can request completion list with a scope", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "var a = 10; #region controller Console.W #endregion" }] });
            session.openProject(project);
            let completionListResult = await session.getCompletionList("program.cs", 9, "controller");
            completionListResult.should.not.be.null;
        });

        it("can request signature help", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "Console.Write()" }] });
            session.openProject(project);

            let singatureHelpResult = await session.getSignatureHelp("program.cs", 14);
            singatureHelpResult.should.not.be.null;
        });

        it("can request signature help with a scope", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "var a = 10; #region controller Console.Write() #endregion" }] });
            session.openProject(project);

            let singatureHelpResult = await session.getSignatureHelp("program.cs", 14, "controller");
            singatureHelpResult.should.not.be.null;

        });
    });
});