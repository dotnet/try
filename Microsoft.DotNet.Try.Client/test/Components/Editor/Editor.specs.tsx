// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as Adapter from "enzyme-adapter-react-16";
import * as React from "react";
import * as chai from "chai";
import * as classnames from "classnames";
import * as enzyme from "enzyme";
import * as monacoEditor from "monaco-editor";
import * as styles from "../../../src/dot.net.style.app.css";

import { Editor, IEditorProps, IEditorState } from "../../../src/components/Editor";
import { shallow } from "enzyme";

import { IDiagnostic, IWorkspace } from "../../../src/IState";
import ReactMonacoEditor from "react-monaco-editor";
import { expect } from "chai";
import { Position } from "../../DummyMonacoEditor";
import MlsClientSimulator from "../../mlsClient/mlsClient.simulator";
import { verifyAsJson } from "../../verifyAsJSON";

enzyme.configure({ adapter: new Adapter() });

chai.should();

let stubMonacoModule: any = {
    languages: {
        registerCompletionItemProvider() { },
        registerSignatureHelpProvider() { }
    }
};

describe("{ Editor }", () => {
    it("contains a <MonacoEditor /> component", () => {
        const wrapper = shallow(<Editor />);
        wrapper.find(ReactMonacoEditor).should.have.length(1);
    });

    it("triggers sourceCodeDidChange when <MonacoEditor /> signals onChange", () => {
        var newCode = "new code";
        var returnedCode = "";

        const wrapper = shallow(<Editor sourceCodeDidChange={(newValue) => {
            returnedCode = newValue;
        }} />);

        wrapper.find(ReactMonacoEditor)
            .first()
            .props()
            .onChange(newCode, null);
        returnedCode.should.equal(newCode);
    });

    it("passes editorOptions to the Monaco Editor", () => {
        var editorOptions: any = {
            some: "option"
        };
        const wrapper = shallow(<Editor editorOptions={editorOptions} />);
        wrapper.find(ReactMonacoEditor)
            .first()
            .props()
            .options.should.deep.equal(editorOptions);
    });

    it("shows squiggles after run", () => {
        var returnedDiagnostics: IDiagnostic[] = null;
        let expectedDiagnostics = [
            {
                start: 10,
                end: 20,
                message: "Bad",
                severity: 2
            }
        ];
        const wrapper = shallow(<Editor />);
        wrapper.setState({
            diagnosticsAdapter: {
                setDiagnostics: (diag: IDiagnostic[]) => { returnedDiagnostics = diag; },
                clearDiagnostics: () => { returnedDiagnostics = [] }
            }
        });
        wrapper.setProps({
            runState: {
                diagnostics: expectedDiagnostics
            }
        });
        returnedDiagnostics.should.equal(expectedDiagnostics);
    });

    it("removes squiggles after a run without diagnostics", () => {
        var returnedDiagnostics: IDiagnostic[] = null;
        let expectedDiagnostics = [
            {
                start: 10,
                end: 20,
                message: "Bad",
                severity: 2
            }
        ];
        const wrapper = shallow(<Editor />);
        wrapper.setState({
            diagnosticsAdapter: {
                setDiagnostics: (diag: IDiagnostic[]) => { returnedDiagnostics = diag; },
                clearDiagnostics: () => { returnedDiagnostics = []; }
            }
        });
        wrapper.setProps({
            runState: {
                diagnostics: expectedDiagnostics
            }
        });
        returnedDiagnostics.should.equal(expectedDiagnostics);
        wrapper.setProps({
            runState: {
                diagnostics: []
            }
        });
        expect(returnedDiagnostics).to.be.empty;
    });

    it("componentDidUpdate invokes editor.updateOptions when editorOptions changes", () => {
        var editorOptions: any = {
            some: "option"
        };
        var actualOptions = {};
        const wrapper = shallow<IEditorProps, IEditorState>(<Editor />);
        var instance: any = wrapper.instance();
        var oldProps = instance.props;
        var codeEditor: any = {
            updateOptions: (options: monacoEditor.editor.IEditorOptions) => { actualOptions = options; },
            getModel: () => null as monacoEditor.editor.ICodeEditor
        };
        instance.editorDidMount(codeEditor, stubMonacoModule);
        wrapper.setProps({ editorOptions: editorOptions });
        instance.componentDidUpdate(oldProps, undefined, undefined);
        actualOptions.should.deep.equal(editorOptions);
    });

    it("editorDidMount registers editor.layout() to window.resize()", () => {
        const wrapper = shallow(<Editor />);
        let instance: any = wrapper.instance();
        let layoutCalled = false;
        let handler: () => void;
        let resizeListener: any = (type: string, listener: () => void) => {
            if (type === "resize") {
                handler = listener;
            }
        };
        window.addEventListener = resizeListener;
        var codeEditor: any = {
            layout: () => { layoutCalled = true; },
            getModel: () => null as monacoEditor.editor.ICodeEditor
        };
        instance.editorDidMount(codeEditor, stubMonacoModule);
        handler();
        expect(layoutCalled).to.be.true;
    });

    it("does not contain a <MonacoEditor /> component when editor was hidden", () => {
        const wrapper = shallow(<Editor showEditor={false} />);
        wrapper.find(ReactMonacoEditor).should.have.length(0);
    });
    it("highlights current line if instrumentation enabled", () => {
        const wrapper = shallow(<Editor />);
        const instance: any = wrapper.instance();
        var decorationsSet: any = [];
        const codeEditor = {
            getModel: () => null as monacoEditor.editor.ICodeEditor,
            deltaDecorations(_unused: string[], decorations: monacoEditor.editor.IModelDeltaDecoration[]) { decorationsSet = decorations; },
            revealLineInCenterIfOutsideViewport: (_unused: number) => null as any
        };
        instance.editorDidMount(codeEditor, stubMonacoModule);
        wrapper.setProps({
            runState: {
                currentInstrumentationStep: 0,
                instrumentation: [{
                    filePosition: {
                        line: 1
                    }
                }]
            }
        });
        instance.componentDidUpdate(instance.props);
        expect(decorationsSet).to.deep.equal([{
            range: {
                startLineNumber: 2,
                startColumn: 0,
                endLineNumber: 2,
                endColumn: 0
            },
            options: {
                isWholeLine: true,
                className: classnames(styles.highlightedLine)
            }
        }]);
    });

    it("sets code lens if there are variables", () => {
        const wrapper = shallow(<Editor />);
        const instance: any = wrapper.instance();

        wrapper.setProps({
            runState: {
                instrumentation: sampleInstrumentationData.instrumentationStates,
                variableLocations: sampleInstrumentationData.variableLocations,
                currentInstrumentationStep: 0
            }
        });

        var codeLensProvider: any = [];
        const monacoModule: any = {
            languages: {
                ...stubMonacoModule.languages,
                registerCodeLensProvider: (_unused: any, codeLens: any) => {
                    codeLensProvider = codeLens;
                    return 42;
                }
            }
        };
        const codeEditor: any = {
            getModel: () => null as monacoEditor.editor.ICodeEditor,
            deltaDecorations: (a: any) => a
        };
        instance.editorDidMount(codeEditor, monacoModule);
        instance.componentDidUpdate(instance.props);
        codeLensProvider.provideCodeLenses(null, null).should.have.deep.members([
            {
                range: {
                    startLineNumber: 2,
                    startColumn: 1,
                    endLineNumber: 3,
                    endColumn: 1
                },
                command: {
                    id: null,
                    title: "a = 1"
                }
            },
            {
                range: {
                    startLineNumber: 1,
                    startColumn: 1,
                    endLineNumber: 2,
                    endColumn: 1
                },
                command: {
                    id: null,
                    title: "a = 1"
                }
            }
        ]);
    });

    it("removes instrumentation if text changed", () => {
        const wrapper = shallow(<Editor />);
        const instance: any = wrapper.instance();

        wrapper.setProps({
            runState: {
                instrumentation: sampleInstrumentationData.instrumentationStates,
                variableLocations: sampleInstrumentationData.variableLocations,
                currentInstrumentationStep: 0
            }
        });

        var codeLensDisposed = false;
        const monacoModule: any = {
            languages: {
                ...stubMonacoModule.languages,
                registerCodeLensProvider: (_unused: any, _codeLens: any) => {
                    return {
                        dispose: () => { codeLensDisposed = true; }
                    };
                }
            }
        };

        var decorationsSet = {};
        const codeEditor: any = {
            getModel: () => null as monacoEditor.editor.ICodeEditor,
            deltaDecorations: (_unused: string[], decorations: monacoEditor.editor.IModelDeltaDecoration) => {
                decorationsSet = decorations;
            }
        };

        instance.editorDidMount(codeEditor, monacoModule);
        instance.componentDidUpdate(instance.props);
        instance.onChange(null, null);

        decorationsSet.should.deep.equal([]);
        codeLensDisposed.should.equal(true);
    });

    it("removes instrumentation if instrumentation disabled", () => {
        const wrapper = shallow(<Editor />);
        const instance: any = wrapper.instance();

        wrapper.setProps({
            runState: {
                instrumentation: sampleInstrumentationData.instrumentationStates,
                variableLocations: sampleInstrumentationData.variableLocations,
                currentInstrumentationStep: 0
            },
            instrumentationEnabled: true
        });

        var codeLensDisposed = false;
        const monacoModule: any = {
            languages: {
                ...stubMonacoModule.languages,
                registerCodeLensProvider: (_unused: any, _: any) => {
                    return {
                        dispose: () => { codeLensDisposed = true; }
                    };
                }
            }
        };

        var decorationsSet = {};
        const codeEditor: any = {
            getModel: () => null as monacoEditor.editor.ICodeEditor,
            deltaDecorations: (_unused: string[], decorations: monacoEditor.editor.IModelDeltaDecoration) => {
                decorationsSet = decorations;
            }
        };

        instance.editorDidMount(codeEditor, monacoModule);
        instance.componentDidUpdate(instance.props);

        wrapper.setProps({
            runState: {
                instrumentation: sampleInstrumentationData.instrumentationStates,
                variableLocations: sampleInstrumentationData.variableLocations,
                currentInstrumentationStep: 0
            },
            instrumentationEnabled: false
        });
        instance.componentDidUpdate(instance.props);

        decorationsSet.should.deep.equal([]);
        codeLensDisposed.should.equal(true);
    });

    it("calls revealLineInCenterIfOutsideViewport on next instrumentation step", () => {
        const wrapper = shallow(<Editor />);
        const instance: any = wrapper.instance();
        var expectedLine: any = [];

        const codeEditor = {
            getModel: () => null as monacoEditor.editor.ICodeEditor,
            deltaDecorations(_unused: string[], _unused2: monacoEditor.editor.IModelDeltaDecoration[]) { },
            revealLineInCenterIfOutsideViewport: (line: number) => expectedLine = line
        };

        instance.editorDidMount(codeEditor, stubMonacoModule);
        wrapper.setProps({
            runState: {
                currentInstrumentationStep: 0,
                instrumentation: [{
                    filePosition: {
                        line: 1
                    }
                }]
            }
        });
        instance.componentDidUpdate(instance.props);
        expectedLine.should.equal(1);
    });

    describe("Registers the language services and provides the result to monaco in the correct format", () => {
        const workspace: IWorkspace = {
            workspaceType: "someWorkspace",
            buffers: [],
            language: "csharp"
        };

        const codeEditor = {
            getModel: () => getFakeModel(),
        };

        it("editorDidMount registers completionItemProvider registers the csharp language", async () => {
            let language: string;
            let stubMonacoModule: any = {
                languages: {
                    registerCompletionItemProvider: (type: string) => {
                        language = type;
                    },
                    registerSignatureHelpProvider() { }
                },
                editor: {
                    setModelMarkers: () => { }
                }
            };

            const wrapper = shallow(<Editor workspace={workspace} client={new MlsClientSimulator()} />);
            let instance: any = wrapper.instance();

            instance.editorDidMount(codeEditor, stubMonacoModule);
            expect(language).to.be.equal("csharp");
        });

        it("editorDidMount registers signatureHelpProvider which registers the csharp language", async () => {
            let language: string;
            let stubMonacoModule: any = {
                languages: {
                    registerSignatureHelpProvider: (type: string) => {
                        language = type;
                    },
                    registerCompletionItemProvider() { }
                },
                editor: {
                    setModelMarkers: () => { }
                }
            };

            const wrapper = shallow(<Editor workspace={workspace} client={new MlsClientSimulator()} />);
            let instance: any = wrapper.instance();

            instance.editorDidMount(codeEditor, stubMonacoModule);
            expect(language).to.be.equal("csharp");
        });

        describe("Contract tests", () => {
            it("The completion contract with Monaco has not been broken", async () => {
                let completionProvider: monacoEditor.languages.CompletionItemProvider;
                let stubMonacoModule: any = {
                    languages: {
                        registerCompletionItemProvider: (_: string, complProvider: monacoEditor.languages.CompletionItemProvider) => {
                            completionProvider = complProvider;
                        },
                        registerSignatureHelpProvider() { }
                    },
                    editor: {
                        setModelMarkers: () => { }
                    }
                };

                const wrapper = shallow(<Editor workspace={workspace} client={new MlsClientSimulator()} />);
                let instance: any = wrapper.instance();

                instance.editorDidMount(codeEditor, stubMonacoModule);
                completionProvider.should.not.be.null;
                let completionItems = await completionProvider.provideCompletionItems(getFakeModel(), new Position(0, 0), null, null) as monacoEditor.languages.CompletionItem[];
                await verifyAsJson("Completion.approved.txt", completionItems);
            });

            it("The signature help contract with Monaco has not been broken", async () => {
                let signatureHelpProvider: monacoEditor.languages.SignatureHelpProvider;
                let stubMonacoModule: any = {
                    languages: {
                        registerSignatureHelpProvider: (_type: string, sigHelpProvider: monacoEditor.languages.SignatureHelpProvider) => {
                            signatureHelpProvider = sigHelpProvider;
                        },
                        registerCompletionItemProvider() { }
                    },
                    editor: {
                        setModelMarkers: () => { }
                    }
                };

                const wrapper = shallow(<Editor workspace={workspace} client={new MlsClientSimulator()} />);
                let instance: any = wrapper.instance();

                instance.editorDidMount(codeEditor, stubMonacoModule);
                signatureHelpProvider.should.not.be.null;
                let signatureHelp = await signatureHelpProvider.provideSignatureHelp(getFakeModel(), new Position(0, 0), null) as monacoEditor.languages.SignatureHelp;
                await verifyAsJson("SignatureHelp.approved.txt", signatureHelp);
            });
        });
    });
});



const sampleInstrumentationData = {
    instrumentationStates: [{
        locals: [{
            name: "a",
            value: "1",
            declaredAt: {
                start: 1,
                end: 2
            }
        }]
    }],
    variableLocations: [{
        name: "a",
        locations: [
            {
                startLine: 1,
                endLine: 1,
                startColumn: 1,
                endColumn: 2
            },
            {
                startLine: 0,
                endLine: 0,
                startColumn: 1,
                endColumn: 2
            }
        ],
        declaredAt: { start: 1, end: 2 }
    }]
};

function getFakeModel(): monacoEditor.editor.ITextModel {
    return {
        uri: null,
        id: null,
        getOptions: () => null,
        getVersionId: () => null,
        getAlternativeVersionId: () => null,
        setValue: () => null,
        getValue: () => null,
        getValueLength: () => null,
        getValueInRange: () => null,
        getValueLengthInRange: () => null,
        getLineCount: () => null,
        getLineContent: () => null,
        getLineLength: () => null,
        getLinesContent: () => null,
        getEOL: () => null,
        getLineMinColumn: () => null,
        getLineMaxColumn: () => null,
        getLineFirstNonWhitespaceColumn: () => null,
        getLineLastNonWhitespaceColumn: () => null,
        validatePosition: () => null,
        modifyPosition: () => null,
        validateRange: () => null,
        getOffsetAt: () => null,
        getPositionAt: () => new Position(0,0),
        getFullModelRange: () => null,
        isDisposed: () => null,
        findMatches: () => null as monacoEditor.editor.FindMatch[],
        findNextMatch: () => null,
        findPreviousMatch: () => null,
        getWordAtPosition: () => null as monacoEditor.editor.IWordAtPosition,
        getWordUntilPosition: () => null as  monacoEditor.editor.IWordAtPosition,
        getModeId: () => null as string,
        deltaDecorations: () => null,
        getDecorationOptions: () => null,
        getDecorationRange: () => null,
        getLineDecorations: () => null,
        getLinesDecorations: () => null,
        getDecorationsInRange: () => null,
        getAllDecorations: () => null,
        getOverviewRulerDecorations: () => null,
        normalizeIndentation: () => null,
        getOneIndent: () => null,
        updateOptions: () => null,
        detectIndentation: () => null,
        pushStackElement: () => null,
        pushEditOperations: () => null,
        pushEOL: () => null,
        applyEdits: () => null,
        setEOL: () => null,
        onDidChangeContent: () => null,
        onDidChangeDecorations: () => null,
        onDidChangeOptions: () => null,
        onDidChangeLanguage: () => null,
        onDidChangeLanguageConfiguration: () => null,
        onWillDispose: () => null,
        dispose: () => null,
    }
}
