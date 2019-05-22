// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, createProject } from "../src/index";
import { buildSimpleIFrameDom, getEditorIFrame } from "./domUtilities";
import { JSDOM } from "jsdom";
import { createReadySession } from "./sessionFactory";
chai.should();

describe("a user", () => {
    let configuration: Configuration;
    let dom: JSDOM;
    let editorIFrame: HTMLIFrameElement;

    beforeEach(() => {
        configuration = { hostOrigin: "https://docs.microsoft.com" };
        dom = buildSimpleIFrameDom(configuration);
        editorIFrame = getEditorIFrame(dom);
    });

    describe("with a trydotnet session", () => {
        it("can open a project", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject("console", [{ name: "program.cs", content: "" }]);
            await session.openProject(project);
        });
    });
});