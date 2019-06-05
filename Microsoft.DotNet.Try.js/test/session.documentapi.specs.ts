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
        it("can open a document", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
            await session.openProject(project);
            let document = await session.openDocument({ fileName: "program.cs" });

            document.id().should.equal("program.cs");
            document.getContent().should.equal("file content");
        });

        it("creates a empty document when the project does not have a matching file", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
            await session.openProject(project);

            let document = await session.openDocument({ fileName: "program_two.cs" });
            expect(document).to.not.be.null;
            document.id().should.equal("program_two.cs");
            document.getContent().should.equal("");
        });

        it("creates a empty document when using region and the project does not have a matching file", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
            await session.openProject(project);

            let document = await session.openDocument({ fileName: "program_two.cs", region: "controller" });
            expect(document).to.not.be.null;
            document.id().should.equal("program_two.cs@controller");
            document.getContent().should.equal("");
        });

        it("can open a document with region as identifier", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "//pre\n#region controller\n//content\n e#endregion\n//post/n" }] });
            await session.openProject(project);

            registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");
            registerForRegionFromFile(configuration, editorIFrame, dom.window, (files: SourceFile[]) => {
                if (files) {
                    return [{ id: "program.cs@controller", content: "//content" }]
                }
                return null;
            })
            let document = await session.openDocument({ fileName: "program.cs", region: "controller" });
            document.id().should.equal("program.cs@controller");
            document.getContent().should.equal("//content");
        });

        it("can open a document and bind it immediately to an editor", async () => {
            let editorState = { content: "", documentId: "" };
            let session = await createReadySession(configuration, editorIFrame, dom.window);
            let defaultEditor = session.getTextEditor();
            let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
            registerForRequestIdGeneration(configuration, editorIFrame, dom.window, _r => "TestRun0");
            await session.openProject(project);
            registerForEditorMessages(configuration, editorIFrame, dom.window, editorState);
            await session.openDocument({ fileName: "program.cs", editorId: defaultEditor.id() });
            await wait(1000);
            editorState.content.should.equal("file content");
            editorState.documentId.should.equal("program.cs");
        });


        describe("and with a document", () => {
            it("can set the content and affect editor", async () => {
                let editorState = { content: "", documentId: "" };
                let session = await createReadySession(configuration, editorIFrame, dom.window);
                let defaultEditor = session.getTextEditor();
                let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
                await session.openProject(project);
                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, _r => "TestRun1");

                await session.openProject(project);
                let document = await session.openDocument({ fileName: "program.cs", editorId: defaultEditor.id() });
                registerForEditorMessages(configuration, editorIFrame, dom.window, editorState);
                await document.setContent("new content");
                document.getContent().should.equal("new content");
                await wait(1000);
                editorState.content.should.equal("new content");
            });

            it("can track editor changes", async () => {
                let editorState = { content: "", documentId: "" };
                let session = await createReadySession(configuration, editorIFrame, dom.window);
                let defaultEditor = session.getTextEditor();
                let project = await createProject({ packageName: "console", files: [{ name: "program.cs", content: "file content" }] });
                await session.openProject(project);
                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, _r => "TestRun2");
                registerForEditorMessages(configuration, editorIFrame, dom.window, editorState);
                let document = await session.openDocument({ fileName: "program.cs", editorId: defaultEditor.id() });
                let editor = session.getTextEditor();
                await editor.setContent("new editor content");
                await wait(1000);
                document.getContent().should.equal("new editor content");
            });

            it("can track editor changes with multiple editors", async () => {
                dom = buildMultiIFrameDom(configuration);
                let editorIFrames = getEditorIFrames(dom);
                let editorStates: { [key: string]: EditorState } = {};

                let session = await createReadySessionWithMultipleEditors(configuration, editorIFrames, dom.window);

                let project = await createProject(
                    {
                        packageName: "console", files: [
                            { name: "program.cs", content: "the program" },
                            { name: "otherFile.cs", content: "other file content" }
                        ]
                    });

                await session.openProject(project);

                for (let iframe of editorIFrames) {
                    editorStates[iframe.dataset.trydotnetEditorId] = { content: "", documentId: "" };
                    registerForRequestIdGeneration(configuration, iframe, dom.window, _r => "TestRun2");
                    registerForEditorMessages(configuration, iframe, dom.window, editorStates[iframe.dataset.trydotnetEditorId]);
                }

                let editorIds = Object.getOwnPropertyNames(editorStates);
                let lastIndex = editorIds.length - 1;
                let programDocument = await session.openDocument({ editorId: editorIds[lastIndex], fileName: "program.cs" });
                let otherFileDocument = await session.openDocument({ editorId: editorIds[0], fileName: "otherFile.cs" });

                raiseTextChange(configuration, dom.window, "new editor content", programDocument.id());
                raiseTextChange(configuration, dom.window, "new content in the other file!", otherFileDocument.id());

                await wait(1000);

                programDocument.getContent().should.equal("new editor content");
                otherFileDocument.getContent().should.equal("new content in the other file!");
            });

            it("can dispatch editor change messages with multiple editors", async () => {
                dom = buildDoubleIFrameDom(configuration);
                let editorIFrames = getEditorIFrames(dom);
                let editorMessageStacks: { [key: string]: ApiMessage[] } = {};

                let session = await createReadySessionWithMultipleEditors(configuration, editorIFrames, dom.window);

                let project = await createProject(
                    {
                        packageName: "console", files: [
                            { name: "program.cs", content: "the program" },
                            { name: "otherFile.cs", content: "other file content" }
                        ]
                    });

                await session.openProject(project);

                for (let iframe of editorIFrames) {
                    editorMessageStacks[iframe.dataset.trydotnetEditorId] = [];
                    registerForRequestIdGeneration(configuration, iframe, dom.window, _r => "TestRun2");
                    trackSetWorkspaceRequests(configuration, iframe, dom.window, editorMessageStacks[iframe.dataset.trydotnetEditorId]);
                }

                let editorIds = Object.getOwnPropertyNames(editorMessageStacks);
                let lastIndex = editorIds.length - 1;
                let programDocument = await session.openDocument({ editorId: editorIds[lastIndex], fileName: "program.cs" });
                let otherFileDocument = await session.openDocument({ editorId: editorIds[0], fileName: "otherFile.cs" });

                raiseTextChange(configuration, dom.window, "new content in program!", programDocument.id());
                raiseTextChange(configuration, dom.window, "new content in the other file!", otherFileDocument.id());

                await wait(1100);

                let programEditorMessages = editorMessageStacks[editorIds[lastIndex]];
                let otherFileEditorMessages = editorMessageStacks[editorIds[0]];

                programEditorMessages.length.should.be.greaterThan(0);
                otherFileEditorMessages.length.should.be.greaterThan(0);

                let lastProgramEditorMessage = <{
                    type: typeof SET_WORKSPACE_REQUEST,
                    workspace: any,
                    bufferId: string,
                    requestId: string
                }>programEditorMessages[programEditorMessages.length - 1];

                let lastOtherFileEditorMessage = <{
                    type: typeof SET_WORKSPACE_REQUEST,
                    workspace: any,
                    bufferId: string,
                    requestId: string
                }>otherFileEditorMessages[otherFileEditorMessages.length - 1];

                lastProgramEditorMessage.type.should.equal(SET_WORKSPACE_REQUEST);
                lastProgramEditorMessage.type.should.equal(lastOtherFileEditorMessage.type);

                lastProgramEditorMessage.workspace.should.deep.equal(lastOtherFileEditorMessage.workspace);
                lastProgramEditorMessage.bufferId.should.be.equal(programDocument.id());
                lastOtherFileEditorMessage.bufferId.should.be.equal(otherFileDocument.id());

                lastProgramEditorMessage.workspace.buffers.should.deep.equal(lastOtherFileEditorMessage.workspace.buffers);
                (<IWorkspace>(lastProgramEditorMessage.workspace)).buffers.find(b => b.id === "program.cs").content.should.be.equal("new content in program!");
                (<IWorkspace>(lastProgramEditorMessage.workspace)).buffers.find(b => b.id === "otherFile.cs").content.should.be.equal("new content in the other file!");
            });
        });
    });
});