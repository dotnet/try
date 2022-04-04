// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Document } from "../../src/internals/document";
import { FakeMonacoTextEditor } from "../fakes/fakeMonacoTextEditor";

chai.should();


describe("a document", () => {

    it("is not marked as modified at creation", () => {
        let document = new Document("program.cs", "content");
        document.isModified.should.be.false;
    });

    it("is not marked as modified when the content is changed from editor", async () => {
        let document = new Document("program.cs", "content");
        let editor = new FakeMonacoTextEditor("0");
        await document.bindToEditor(editor);
        editor.raiseTextEvent("other content");
        document.isModified.should.be.false;
        document.getContent().should.be.equal("other content");
    });

    it("is active if bound to an editor", async () => {
        let document = new Document("program.cs", "content");
        let editor = new FakeMonacoTextEditor("0");
        document.isActiveInEditor().should.be.false;
        await document.bindToEditor(editor);
        document.isActiveInEditor().should.be.true;
    });

    it("is marked as modified when the content is changed via setContent", async () => {
        let document = new Document("program.cs", "content");        
        await document.setContent("modified content");
        document.isModified.should.be.true;
    });

});