// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Workspace } from "../../src/internals/workspace";
import { createProject } from "../../src";
import { FakeMonacoTextEditor } from "../fakes/fakeMonacoTextEditor";
import { FakeMessageBus } from "../fakes/fakeMessageBus";
import { FakeIdGenerator } from "../fakes/fakeIdGenerator";
import { expect } from "chai";
chai.should();

describe("a workspace", () => {

    it("is not marked as modified at creation", () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        ws.isModified().should.be.false;
    });

    it("is marked as modified when propulated from a project", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject("console", [{ name: "program.cs", content: "the program" }, { name: "otherFile.cs", content: "other file content" }]);
        ws.fromProject(project);
        ws.isModified().should.be.true;
    });

    it("is marked as modified when a document is opened", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject("console", [{ name: "program.cs", content: "the program" }, { name: "otherFile.cs", content: "other file content" }]);
        ws.fromProject(project);
        await ws.openDocument({ fileName: "program.cs" });
        ws.isModified().should.be.true;
    });

    it("is not marked as modified when is exported as setWorkspaceRequest object", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject("console", [{ name: "program.cs", content: "the program" }, { name: "otherFile.cs", content: "other file content" }]);
        ws.fromProject(project);
        ws.isModified().should.be.true;
        ws.toSetWorkspaceRequests();
        ws.isModified().should.be.false;
    });

    it("is not marked as modified when a document is opened again", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject("console", [{ name: "program.cs", content: "the program" }, { name: "otherFile.cs", content: "other file content" }]);
        ws.fromProject(project);
        ws.isModified().should.be.true;
        let doc = await ws.openDocument({ fileName: "program.cs" });
        ws.toSetWorkspaceRequests();
        ws.isModified().should.be.false;
        doc = await ws.openDocument({ fileName: "program.cs" });
    });

    it("is marked as modified when a document is opened and  the content is changed", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject("console", [{ name: "program.cs", content: "the program" }, { name: "otherFile.cs", content: "other file content" }]);
        ws.fromProject(project);
        let doc = await ws.openDocument({ fileName: "program.cs" });
        doc.setContent("modified content");
        ws.isModified().should.be.true;
    });

    it("generates set workspace request object", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject("console", [{ name: "program.cs", content: "the program" }, { name: "otherFile.cs", content: "other file content" }]);
        ws.fromProject(project);
        let doc = await ws.openDocument({ fileName: "program.cs" });
        doc.setContent("modified content");
        let wsr = ws.toSetWorkspaceRequests();
        wsr.should.not.be.null;
        wsr.workspace.should.not.be.null;
        wsr.workspace.buffers[0].should.not.be.null;
    });

    it("generates set workspace request object with active bufferId equal to the document currently open in an editor", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject(
            "console",
            [
                { name: "program.cs", content: "the program" },
                { name: "otherFile.cs", content: "other file content" }
            ]);

        ws.fromProject(project);
        let editorZero = new FakeMonacoTextEditor("0");
        let editorOne = new FakeMonacoTextEditor("1");
        let programDocument = await ws.openDocument({ fileName: "program.cs", textEditor: editorZero });
        let otherFileDocument = await ws.openDocument({ fileName: "otherFile.cs", textEditor: editorOne });
        programDocument.setContent("modified content");
        let wsr = ws.toSetWorkspaceRequests();

        wsr.workspace.should.not.be.null;
        wsr.workspace.buffers[0].should.not.be.null;
        wsr.workspace.buffers[1].should.not.be.null;
        wsr.bufferIds[editorZero.id()].should.be.equal("program.cs");
        wsr.bufferIds[editorOne.id()].should.be.equal("otherFile.cs");
    });

    it("can open a document and set its content", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject(
            "console",
            [
                { name: "program.cs", content: "the program" },
                { name: "otherFile.cs", content: "other file content" }
            ]);

        ws.fromProject(project);
        let doc = await ws.openDocument({ fileName: "program.cs", content: "content override" });
        expect(doc).not.to.be.null;
        doc.getContent().should.be.equal("content override");
    });

    it("can open a document in the editor and set its content", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject(
            "console",
            [
                { name: "program.cs", content: "the program" },
                { name: "otherFile.cs", content: "other file content" }
            ]);

        let editorZero = new FakeMonacoTextEditor("0");
        ws.fromProject(project);
        let doc = await ws.openDocument({ fileName: "program.cs", textEditor: editorZero, content: "content override" });
        expect(doc).not.to.be.null;
        doc.getContent().should.be.equal("content override");
        doc.currentEditor().should.not.be.null;
        doc.currentEditor().id().should.be.equal("0");
        editorZero.content.should.be.equal(doc.getContent());
    });

    it("can open a document in one edtior at a time", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject(
            "console",
            [
                { name: "program.cs", content: "the program" },
                { name: "otherFile.cs", content: "other file content" }
            ]);

        ws.fromProject(project);
        let editorZero = new FakeMonacoTextEditor("0");
        let editorOne = new FakeMonacoTextEditor("1");
        let doc = await ws.openDocument({ fileName: "program.cs", textEditor: editorZero });

        doc.isActiveInEditor().should.be.true;
        doc.currentEditor().should.not.be.null;
        doc.currentEditor().id().should.be.equal("0");

        doc = await ws.openDocument({ fileName: "program.cs", textEditor: editorOne });
        doc.isActiveInEditor().should.be.true;
        doc.currentEditor().should.not.be.null;
        doc.currentEditor().id().should.be.equal("1");
    });

    it("can open documents in different edtiors at same time", async () => {
        let ws = new Workspace(new FakeMessageBus("0"), new FakeIdGenerator());
        let project = await createProject(
            "console",
            [
                { name: "program.cs", content: "the program" },
                { name: "otherFile.cs", content: "other file content" }
            ]);

        ws.fromProject(project);
        let editorZero = new FakeMonacoTextEditor("0");
        let editorOne = new FakeMonacoTextEditor("1");

        let programDocument = await ws.openDocument({ fileName: "program.cs", textEditor: editorZero });
        let otherFileDocument = await ws.openDocument({ fileName: "otherFile.cs", textEditor: editorOne });

        programDocument.isActiveInEditor().should.be.true;
        programDocument.currentEditor().should.not.be.null;
        programDocument.currentEditor().id().should.be.equal("0");

        otherFileDocument.isActiveInEditor().should.be.true;
        otherFileDocument.currentEditor().should.not.be.null;
        otherFileDocument.currentEditor().id().should.be.equal("1");
    });
});
