// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as Adapter from "enzyme-adapter-react-16";
import * as React from "react";
import * as chai from "chai";
import * as enzyme from "enzyme";
import * as monacoEditor from "monaco-editor";

import { shallowWithStore } from "enzyme-redux";
import getStore, { IObservableAppStore } from "../../observableAppStore";

import Editor, { IEditorProps, IEditorState } from "../../../src/components/Editor";
import ReactMonacoEditor, { MonacoEditorProps } from "react-monaco-editor";
import actions from "../../../src/actionCreators/actions";
import sampleThemes from "../../sampleThemes";
enzyme.configure({ adapter: new Adapter() });
chai.should();

describe("<Editor />", () => {
    var store: IObservableAppStore;

    beforeEach(() => {
        store = getStore();
    });

    it("passes down sourceCode", () => {
        store.configure([
            actions.updateWorkspaceBuffer("some source code", "Program.cs")
        ]);

        const wrapper = shallowWithStore(<Editor />, store).first().shallow();

        wrapper.props().value.should.be.equal("some source code");
    });

    it("passes down editorOptions", () => {
        var expectedOptions = {
            scrollBeyondLastLine: false,
            selectOnLineNumbers: false,
        };

        store.configure([
            actions.configureMonacoEditor(expectedOptions, undefined)
        ]);

        const wrapper = shallowWithStore(<Editor />, store).first().shallow();

        wrapper.props().options.should.deep.equal(expectedOptions);
    });

    // it("updates the theme", () => {
    //     var editorTheme = "";

    //     const wrapper = 
    //         shallowWithStore(<Editor />, store)
    //             .dive<IEditorProps, IEditorState>();

    //     wrapper
    //         .props()
    //         .editorDidMount({
    //             updateOptions: () => { },
    //             getModel: () => null as monacoEditor.editor.ICodeEditor
    //         } as any, {
    //             editor: { setTheme: (theme: string) => editorTheme = theme },
    //             languages: {
    //                 registerCompletionItemProvider() { },
    //                 registerSignatureHelpProvider() { }
    //             },
    //         } as any);

    //     store.dispatch(actions.configureMonacoEditor(undefined, "vs-dark"));
    //     editorTheme.should.equal("vs-dark");
    // });

    it("defines themes configured before rendering during editorWillMount", () => {
        var definedThemes: {
            [x: string]: monacoEditor.editor.IStandaloneThemeData;
        } = {};

        var customThemes = sampleThemes.myCustomTheme;

        store.dispatch(actions.defineMonacoEditorThemes(customThemes as any));

        const wrapper = shallowWithStore(<Editor />, store).dive<IEditorProps, IEditorState>();

        wrapper
            .find(ReactMonacoEditor).first()
            .props().editorWillMount({
                editor: {
                    defineTheme: (name: string, theme: monacoEditor.editor.IStandaloneThemeData) => {
                        definedThemes[name] = theme;
                    }
                }
            } as any);
        definedThemes.should.deep.equal(customThemes);
    });

    // it("defines themes configured after rendering immediately", () => {
    //     var definedThemes: {
    //         [x: string]: monacoEditor.editor.IStandaloneThemeData;
    //     } = {};
    //     var customThemes = sampleThemes.myCustomTheme;

    //     const wrapper = shallowWithStore(<Editor />, store).dive<IEditorProps, IEditorState>();

    //     wrapper
    //         .find(ReactMonacoEditor).first()
    //         .props().editorWillMount({
    //             editor: {
    //                 defineTheme: (name: string, theme: monacoEditor.editor.IStandaloneThemeData) => {
    //                     definedThemes[name] = theme;
    //                 }
    //             }
    //         } as any);
    //     store.dispatch(actions.defineMonacoEditorThemes(customThemes as any));
    //     definedThemes.should.deep.equal(customThemes);
    // });

    it("doesn't register a completion provider when the version is < 2", () => {
        store.configure([actions.setVersion(1)]);

        const wrapper = shallowWithStore(<Editor />, store).dive<IEditorProps, IEditorState>();

        var completionItemConfigured = false;
        wrapper
            .find(ReactMonacoEditor)
            .first()
            .props()
            .editorDidMount({
                updateOptions: () => { },
                getModel: () => null as monacoEditor.editor.ICodeEditor
            } as any, {
                editor: { setTheme: (_theme: string) => { } },
                languages: {
                    registerCompletionItemProvider() { completionItemConfigured = true; },
                    registerSignatureHelpProvider() { }
                }
            } as any);
        completionItemConfigured.should.equal(false);
    });

    it("registers a completion provider when the version is >= 2", () => {
        store.configure([actions.setVersion(2)]);
        const wrapper = shallowWithStore(<Editor />, store).dive<IEditorProps, IEditorState>();
        var completionItemConfigured = false;
        wrapper
            .props()
            .editorDidMount({
                updateOptions: () => { },
                getModel: () => null as monacoEditor.editor.ICodeEditor
            } as any, {
                editor: { setTheme: (_theme: string) => { } },
                languages: {
                    registerCompletionItemProvider() { completionItemConfigured = true; },
                    registerSignatureHelpProvider() { }
                }
            } as any);
        completionItemConfigured.should.equal(true);
    });

    it("dispatches UPDATE_WORKSPACE_BUFFER when <MonacoEditor /> signals onChange", () => {
        var newCode = "new code";

        var expectedActions = [
            actions.setActiveBuffer("bufferOne"),
            actions.updateWorkspaceBuffer(newCode, "bufferOne")
        ];

        store.dispatch(actions.setActiveBuffer("bufferOne"));

        const wrapper = shallowWithStore(<Editor />, store)
            .dive<MonacoEditorProps, null>();

        wrapper.props().onChange(newCode, null);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("dispatches NOTIFY_MONACO_READY when <MonacoEditor /> signals onChange", () => {
        var editor = {
            focus: () => { },
            getModel: () => null as monacoEditor.editor.ICodeEditor,
            layout: () => { }
        };
        var expectedActions = [
            actions.notifyMonacoReady(editor)
        ];
        const wrapper = shallowWithStore(<Editor />, store).first().shallow();

        var monacoModule: any = {
            languages: {
                registerCompletionItemProvider() { },
                registerSignatureHelpProvider() { }
            }
        };

        wrapper
            .props()
            .editorDidMount(editor as any, monacoModule);

        store.getActions().should.deep.equal(expectedActions);
    });

    it("does not render the editor when it is hidden", () => {
        store.configure([
            actions.hideEditor()
        ]);

        const wrapper = shallowWithStore(<Editor />, store);

        wrapper.children().should.have.length(0);
    });
});
