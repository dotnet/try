// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import * as chaiAsPromised from "chai-as-promised";
import { Configuration, createSessionWithProjectAndOpenDocument } from "../src/index";
import { buildSimpleIFrameDom, getEditorIFrame } from "./domUtilities";
import { notifyEditorReady, registerForSetWorkspace } from "./messagingMocks";
import * as dotnetInteractive from "@microsoft/dotnet-interactive";

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
            let editorIFrame = getEditorIFrame(dom);
            let awaitableSession = createSessionWithProjectAndOpenDocument(
                configuration,
                [editorIFrame],
                <Window><any>dom.window,
                {
                    package: "console",
                    files: [{ name: "program.cs", content: "" }]
                },
                "program.cs");

            registerForSetWorkspace(configuration, editorIFrame, dom.window, (files) => {
                return files.map(f => {
                    let item: dotnetInteractive.ProjectItem = {
                        relativeFilePath: f.name,
                        regionNames: [],
                        regionsContent: {}
                    };
                    return item;
                });
            });

            notifyEditorReady(configuration, dom.window);
            let session = await awaitableSession;
            session.should.not.be.null;
        });

        it("can create a session with initial project with regions", async () => {
            let dom = buildSimpleIFrameDom(configuration);
            let editorIFrame = getEditorIFrame(dom);


            let awaitableSession = createSessionWithProjectAndOpenDocument(
                configuration,
                [editorIFrame],
                <Window><any>dom.window,
                {
                    package: "console",
                    files: [{ name: "./Program.cs", content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}" },]
                },
                "program.cs");

            registerForSetWorkspace(configuration, editorIFrame, dom.window, (files) => {
                return files.map(f => {
                    let item: dotnetInteractive.ProjectItem = {
                        relativeFilePath: f.name,
                        regionNames: [],
                        regionsContent: {}
                    };
                    return item;
                });
            });

            notifyEditorReady(configuration, dom.window);

            let session = await awaitableSession;
            session.should.not.be.null;
        });
    });

});
