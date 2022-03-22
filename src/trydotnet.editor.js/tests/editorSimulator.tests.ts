// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from "chai";
import { describe } from "mocha";

import * as monacoEditorSimulator from "./monacoEditorSimulator";

describe("editor simulator", () => {
    it("can manipulate empty text", () => {
        let newState = monacoEditorSimulator.modifyText("", { line: 1, column: 1 }, "hello");
        expect(newState.newText).to.equal("hello");
        expect(newState.line).to.equal(1);
        expect(newState.column).to.equal(6);
    });


    it("can edit previous text", () => {
        let newState = monacoEditorSimulator.modifyText("hello !", { line: 1, column: 7 }, "world");
        expect(newState.newText).to.equal("hello world!");
        expect(newState.line).to.equal(1);
        expect(newState.column).to.equal(12);
    });

    it("updates position correctly", () => {
        let newState = monacoEditorSimulator.modifyText("hello world!!", { line: 1, column: 13 }, "\nThank you");
        expect(newState.newText).to.equal("hello world!\nThank you!");
        expect(newState.line).to.equal(2);
        expect(newState.column).to.equal(10);
    });
});