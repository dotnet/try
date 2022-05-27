// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, createProject } from "../src/index";
import { JSDOM } from "jsdom";
import { buildSimpleIFrameDom, getEditorIFrame } from "./domUtilities";
import * as chaiAsPromised from "chai-as-promised";
import { registerForEditorMessages, registerForSetWorkspace } from "./messagingMocks";
import { wait } from "./wait";
import { createReadySession } from "./sessionFactory";
import * as dotnetInteractive from "@microsoft/dotnet-interactive";

import { expect } from "chai";
import { DocumentId } from "../src/internals/document";

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
        it("can open a document", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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
            await session.openProject(project);
            let document = await session.openDocument({ fileName: "program.cs" });

            document.id().toString().should.equal("program.cs");
            document.getContent().should.equal("file content");
        });

        it("creates a empty document when the project does not have a matching file", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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
            await session.openProject(project);

            let document = await session.openDocument({ fileName: "program_two.cs" });
            expect(document).to.not.be.null;
            document.id().toString().should.equal("program_two.cs");
            document.getContent().should.equal("");
        });

        it("creates a empty document when using region and the project does not have a matching file", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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
            await session.openProject(project);

            let document = await session.openDocument({ fileName: "program_two.cs", region: "controller" });
            expect(document).to.not.be.null;
            document.id().toString().should.equal("program_two.cs@controller");
            document.getContent().should.equal("");
        });

        it("can open a document with region as identifier", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "//pre\n#region controller\n//content\n e#endregion\n//post/n" }] });

            registerForSetWorkspace(configuration, editorIFrame, dom.window, (files) => {
                return files.map(f => {
                    let item: dotnetInteractive.ProjectItem = {
                        relativeFilePath: f.name,
                        regionNames: ["controller"],
                        regionsContent: {
                            controller: "//content"
                        }
                    };
                    return item;
                });
            });

            await session.openProject(project);
            let document = await session.openDocument({ fileName: "program.cs", region: "controller" });
            document.id().toString().should.equal("program.cs@controller");
            document.getContent().should.equal("//content");
        });

        it("can open a document with region with no content", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "//pre\n#region controller\n#endregion\n//post/n" }] });

            registerForSetWorkspace(configuration, editorIFrame, dom.window, (files) => {
                return files.map(f => {
                    let item: dotnetInteractive.ProjectItem = {
                        relativeFilePath: f.name,
                        regionNames: ["controller"],
                        regionsContent: {
                            controller: undefined
                        }
                    };
                    return item;
                });
            });

            await session.openProject(project);
            let document = await session.openDocument({ fileName: "program.cs", region: "controller" });
            document.id().toString().should.equal("program.cs@controller");
            document.getContent().should.equal("");
        });

        it("can open a document and bind it immediately to an editor", async () => {
            let editorState = { content: "", documentId: <DocumentId>undefined };
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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
            await session.openProject(project);
            registerForEditorMessages(configuration, editorIFrame, dom.window, editorState);
            await session.openDocument({ fileName: "program.cs" });
            await wait(1000);
            editorState.content.should.equal("file content");
            editorState.documentId.toString().should.equal("program.cs");
        });

        it("can return the open documents", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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
            await session.openProject(project);

            await session.openDocument({ fileName: "program.cs", content: "i am a document" });
            let document = session.getOpenDocument();
            document.should.not.be.null;
            document.getContent().should.equal("i am a document");
        });

        describe("and with a document", () => {
            it("can set the content and affect editor", async () => {
                let editorState = { content: "", documentId: <DocumentId>undefined };
                let session = await createReadySession(configuration, editorIFrame, dom.window);
                let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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

                await session.openProject(project);
                registerForEditorMessages(configuration, editorIFrame, dom.window, editorState); let document = await session.openDocument({ fileName: "program.cs" });
                await document.setContent("new content");
                document.getContent().should.equal("new content");
                await wait(1000);
                editorState.content.should.equal("new content");
            });

            it("can track editor changes", async () => {
                let editorState = { content: "", documentId: <DocumentId>undefined };
                let session = await createReadySession(configuration, editorIFrame, dom.window);
                let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });

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

                await session.openProject(project);
                registerForEditorMessages(configuration, editorIFrame, dom.window, editorState);
                let document = await session.openDocument({ fileName: "program.cs" });
                let editor = session.getTextEditor();
                await editor.setContent("new editor content");
                await wait(1000);
                document.getContent().should.equal("new editor content");
            });
        });
    });
});