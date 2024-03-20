// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import * as chaiAsPromised from "chai-as-promised";
import { Configuration, createSessionWithProjectAndOpenDocument } from "../src/index";
import { buildSimpleIFrameDom, getEditorIFrame } from "./domUtilities";
import { notifyEditorReady, registerForOpeDocument, registerForOpenProject } from "./messagingMocks";
import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";
import { areSameFile } from "../src/documentId";

chai.use(chaiAsPromised);
chai.should();

describe("a user", () => {
    let configuration: Configuration;

    beforeEach(() => {
        configuration = { hostOrigin: "https://learn.microsoft.com" };
    });
    describe("with single iframe", () => {
        it("can create a session with initial project", async () => {
            let dom = buildSimpleIFrameDom(configuration);
            let editorIFrame = getEditorIFrame(dom);
            const project = {
                package: "console",
                files: [{ name: "program.cs", content: "" }]
            };
            let awaitableSession = createSessionWithProjectAndOpenDocument(
                configuration,
                [editorIFrame],
                <Window><any>dom.window,
                project,
                "program.cs");

            registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                return files.map(f => {
                    let item: polyglotNotebooks.ProjectItem = {
                        relativeFilePath: f.relativeFilePath,
                        regionNames: [],
                        regionsContent: {}
                    };
                    return item;
                });
            });

            registerForOpeDocument(configuration, editorIFrame, dom.window, (documentId) => {
                documentId;//?
                let content = project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content ?? "";
                return content;
            });

            notifyEditorReady(configuration, dom.window);
            let session = await awaitableSession;
            session.should.not.be.null;
        });

        it("can create a session with initial project with regions", async () => {
            let dom = buildSimpleIFrameDom(configuration);
            let editorIFrame = getEditorIFrame(dom);

            const project = {
                package: "console",
                files: [{ name: "./Program.cs", content: "\npublic class Program\n{\n    public static void Main(string[] args)\n    {\n        #region REGION_1\n        var a = 123;\n        #endregion\n\n        #region REGION_2\n        var b = 123;\n        #endregion\n    }\n}" },]
            };
            let awaitableSession = createSessionWithProjectAndOpenDocument(
                configuration,
                [editorIFrame],
                <Window><any>dom.window,
                project,
                "program.cs");

            registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                return files.map(f => {
                    let item: polyglotNotebooks.ProjectItem = {
                        relativeFilePath: f.relativeFilePath,
                        regionNames: [],
                        regionsContent: {}
                    };
                    return item;
                });
            });

            registerForOpeDocument(configuration, editorIFrame, dom.window, (documentId) => {
                documentId;//?
                let content = project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content ?? "";
                return content;
            });

            notifyEditorReady(configuration, dom.window);

            let session = await awaitableSession;
            session.should.not.be.null;
        });
    });

});
