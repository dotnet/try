// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, createProject } from "../src/index";
import { JSDOM } from "jsdom";
import { buildSimpleIFrameDom, getEditorIFrame } from "./domUtilities";
import * as chaiAsPromised from "chai-as-promised";
import { registerForEditorMessages, registerForOpeDocument, registerForOpenProject } from "./messagingMocks";
import { wait } from "./wait";
import { createReadySession } from "./sessionFactory";
import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";

import { expect } from "chai";
import { areSameFile, DocumentId } from "../src/documentId";

chai.use(chaiAsPromised);
chai.should();



describe("A user", () => {

    let configuration: Configuration;
    let dom: JSDOM;
    let editorIFrame: HTMLIFrameElement;

    beforeEach(() => {
        configuration = { hostOrigin: "https://learn.microsoft.com" };
        dom = buildSimpleIFrameDom(configuration);
        editorIFrame = getEditorIFrame(dom);
    });
    describe("with a trydotnet session", () => {
        it("can open a document", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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
                return project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content ?? "";
            });

            await session.openProject(project);
            let document = await session.openDocument({ fileName: "program.cs" });

            document.id().toString().should.equal("program.cs");
            document.getContent().should.equal("file content");
        });

        it("creates a empty document when the project does not have a matching file", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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
                return project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content ?? "";
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
                return project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content ?? "";
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

            const items: { [key: string]: polyglotNotebooks.ProjectItem } = {};
            registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                const pi = files.map(f => {
                    let item: polyglotNotebooks.ProjectItem = {
                        relativeFilePath: f.relativeFilePath,
                        regionNames: ["controller"],
                        regionsContent: {
                            controller: "//content"
                        }
                    };
                    return item;
                });

                for (let i of pi) {
                    items[i.relativeFilePath] = i;
                }

                return pi;
            });

            registerForOpeDocument(configuration, editorIFrame, dom.window, (documentId) => {
                documentId;//?
                let content = project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content ?? "";
                if (documentId.regionName) {
                    content = items[documentId.relativeFilePath].regionsContent[documentId.regionName];
                }
                return content;
            });

            await session.openProject(project);
            let document = await session.openDocument({ fileName: "program.cs", region: "controller" });
            document.id().toString().should.equal("program.cs@controller");
            document.getContent().should.equal("//content");
        });

        it("can open a document with region with no content", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "//pre\n#region controller\n#endregion\n//post/n" }] });

            const items: { [key: string]: polyglotNotebooks.ProjectItem } = {};
            registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                const pi = files.map(f => {
                    let item: polyglotNotebooks.ProjectItem = {
                        relativeFilePath: f.relativeFilePath,
                        regionNames: ["controller"],
                        regionsContent: {
                            controller: <string><unknown>undefined
                        }
                    };
                    return item;
                });

                for (let i of pi) {
                    items[i.relativeFilePath] = i;
                }

                return pi;
            });


            registerForOpeDocument(configuration, editorIFrame, dom.window, (documentId) => {
                documentId;//?
                let content = project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content ?? "";
                if (documentId.regionName) {
                    content = items[documentId.relativeFilePath].regionsContent[documentId.regionName];
                }
                return content;
            });


            await session.openProject(project);
            let document = await session.openDocument({ fileName: "program.cs", region: "controller" });
            document.id().toString().should.equal("program.cs@controller");
            document.getContent().should.equal("");
        });

        it("can open a document and bind it immediately to an editor", async () => {
            let editorState = { content: "", documentId: <DocumentId><unknown>undefined };
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });

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

            await session.openProject(project);

            await session.openDocument({ fileName: "program.cs", content: "i am a document" });
            let document = session.getOpenDocument();
            document.should.not.be.null;
            document.getContent().should.equal("i am a document");
        });

        describe("and with a document", () => {
            it("can set the content and affect editor", async () => {
                let editorState = { content: "", documentId: <DocumentId><unknown>undefined };
                let session = await createReadySession(configuration, editorIFrame, dom.window);
                let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
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

                await session.openProject(project);
                registerForEditorMessages(configuration, editorIFrame, dom.window, editorState); let document = await session.openDocument({ fileName: "program.cs" });
                await document.setContent("new content");
                document.getContent().should.equal("new content");
                await wait(1000);
                editorState.content.should.equal("new content");
            });

            it("can track editor changes", async () => {
                let editorState = { content: "", documentId: <DocumentId><unknown>undefined };
                let session = await createReadySession(configuration, editorIFrame, dom.window);
                let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });

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
                    return project.files.find(f => areSameFile(f.name, documentId.relativeFilePath))?.content || "";
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