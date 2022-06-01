// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { MonacoTextEditor } from "../../src/internals/monacoTextEditor";
import { FakeMessageBus } from "../fakes/fakeMessageBus";
import { FakeIdGenerator } from "../fakes/fakeIdGenerator";
import * as newContract from "../../src/newContract";

chai.should();

describe("a monaco editor", () => {

    let bus: FakeMessageBus;
    let idGenerator: FakeIdGenerator;
    let editor: MonacoTextEditor;

    beforeEach(() => {
        bus = new FakeMessageBus("test bus");
        idGenerator = new FakeIdGenerator();
        editor = new MonacoTextEditor(bus, idGenerator);
    });

    it("can set the theme as string", () => {
        let messages: { type: string, requestId?: string }[] = [];
        bus.requests.subscribe({ next: m => messages.push(m) });
        editor.setTheme("different theme");
        messages.should.not.be.empty;
        messages[0].type.should.equal(newContract.ConfigureMonacoEditorType);
    });

    it("can set the theme as object", () => {
        let messages: { type: string, requestId?: string }[] = [];
        bus.requests.subscribe({ next: m => messages.push(m) });
        editor.setTheme({
            name: "different theme",
            monacoEditorTheme: {
                base: 'vs-dark',
                inherit: true,
                rules: [{
                    token: 'comment',
                    foreground: 'red',
                    fontStyle: 'italic'
                }]
            }
        });
        messages.should.not.be.empty;
        messages[0].type.should.equal(newContract.DefineMonacoEditorThemesType);
        (<any>messages[0]).themes.should.deep.equal({
            "different theme": {
                base: 'vs-dark',
                inherit: true,
                rules: [{
                    token: 'comment',
                    foreground: 'red',
                    fontStyle: 'italic'
                }]
            }
        });
        messages[1].type.should.equal(newContract.ConfigureMonacoEditorType);
    });

    it("can set the editor options", () => {
        let messages: { type: string, requestId?: string }[] = [];
        bus.requests.subscribe({ next: m => messages.push(m) });
        editor.setOptions({
            minimap: {
                enabled: false
            }
        });
        messages.should.not.be.empty;
        messages[0].type.should.equal(newContract.ConfigureMonacoEditorType);
        messages[0].type.should.equal(newContract.ConfigureMonacoEditorType);
        (<any>messages[0]).editorOptions.minimap.should.deep.equal(
            {
                enabled: false
            });
    });

    it("can be configured", () => {
        let messages: { type: string, requestId?: string }[] = [];
        bus.requests.subscribe({ next: m => messages.push(m) });
        editor.configure({
            theme: "different theme",
            options: {
                minimap: {
                    enabled: false
                }
            }
        });
        messages.should.not.be.empty;
        messages[0].type.should.equal(newContract.ConfigureMonacoEditorType);
        (<any>messages[0]).editorOptions.minimap.should.deep.equal(
            {
                enabled: false
            });
        messages[1].type.should.equal(newContract.ConfigureMonacoEditorType);
        (<any>messages[1]).theme.should.equal("different theme");
    });

});