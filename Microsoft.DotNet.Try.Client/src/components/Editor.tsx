// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";

import { Dispatch } from "redux";
import { connect, MapDispatchToProps } from "react-redux";
import IMlsClient, { IRunRequest, IWorkspaceRequest } from "../IMlsClient";
import IState, { IRunState, IWorkspace, IDiagnostic, ICompileState } from "../IState";

import DiagnosticsAdapter from "./DiagnosticsAdapter";
import InstrumentationAdapter from "./InstrumentationAdapter";
import * as monacoEditor from "monaco-editor";
import ReactMonacoEditor, { EditorDidMount, EditorWillMount } from "react-monaco-editor";
import actions from "../actionCreators/actions";

import deepEqual = require("deep-equal");
import { cloneWorkspace, setBufferContent } from "../workspaces";
import ICompletionItem from "../ICompletionItem";
import { CreateProjectFromGistRequest, CreateRegionsFromFilesRequest } from "../clientApiProtocol";

import { Subject, interval } from "rxjs";
import { debounce } from "rxjs/operators";
import { SupportedLanguages } from "../constants/supportedLanguages";

export interface IEditorProps {
    completionProvider?: string;
    editorDidMount?: EditorDidMount;
    editorWillMount?: EditorWillMount;
    editorOptions?: monacoEditor.editor.IEditorOptions;
    showEditor?: boolean;
    sourceCode?: string;
    sourceCodeDidChange?: (sourceCode: string, bufferId: string) => void;
    theme?: string;
    themes?: { [x: string]: monacoEditor.editor.IStandaloneThemeData };
    client?: IMlsClient;
    showCompletions?: boolean;
    runState?: IRunState;
    compileState?: ICompileState;
    activeBufferId?: string;
    workspace?: IWorkspace;
    instrumentationEnabled?: boolean;
    updateDiagnostics?: (diagnostics: IDiagnostic[]) => void;
    editorLanguage?: SupportedLanguages;
}

export interface IEditorState {
    editor: monacoEditor.editor.IEditor;
    defineTheme: (name: string, value: monacoEditor.editor.IStandaloneThemeData) => void;
    setTheme: (t: string) => void;
    diagnosticsAdapter: DiagnosticsAdapter;
    instrumentationAdapter: InstrumentationAdapter;
    monacoModule: typeof monacoEditor;
    editorLanguage: SupportedLanguages;
}

export interface TextChangedEvent {
    text: string;
    position: number;
}

export class Editor extends React.Component<IEditorProps, IEditorState> {
    constructor(props: IEditorProps) {
        super(props);

        this.editorDidMount = this.editorDidMount.bind(this);
        this.editorWillMount = this.editorWillMount.bind(this);
        this.componentDidUpdate = this.componentDidUpdate.bind(this);
        this.defineThemes = this.defineThemes.bind(this);
    }

    private completionPosition: monacoEditor.Position;
    private completionItems: ICompletionItem[];
    private contentChangedChannel: Subject<TextChangedEvent> = new Subject<TextChangedEvent>();

    private editorWillMount: EditorWillMount = (monacoModule: typeof monacoEditor) => {
        let defineTheme = (name: string, theme: monacoEditor.editor.IStandaloneThemeData) => monacoModule.editor.defineTheme(name, theme);

        this.setState({
            ...this.state,
            defineTheme: defineTheme,
            editorLanguage: this.props.editorLanguage
        });

        this.defineThemes(defineTheme);
        this.props.editorWillMount(monacoModule);

    }

    private queueDiagnosticsUpdateRequest = (() => {
        if (this.state && this.state.editor && this.contentChangedChannel) {
            let model = this.state.editor.getModel() as monacoEditor.editor.ITextModel;
            if (model) {
                let text = model.getValue();
                this.contentChangedChannel.next({
                    text: text ? text : "",
                    position: 0
                });
            }
        }
    }).bind(this);

    private textChangedEventHandler = (async (event: TextChangedEvent) => {
        if (this.props && this.props.client && this.props.workspace) {
            let client = this.props.client;
            let workspace = cloneWorkspace(this.props.workspace);
            let activeBuffer = this.props.activeBufferId;
            setBufferContent(workspace, activeBuffer, event.text ? event.text : "");
            let diagnosticsResult = await client.getDiagnostics(workspace, activeBuffer);
            this.setDiagnostics(diagnosticsResult.diagnostics);
        }
    }).bind(this);

    private editorDidMount: EditorDidMount = (
        editor: monacoEditor.editor.IStandaloneCodeEditor,
        monacoModule: typeof monacoEditor) => {
        if (editor.onDidChangeModelContent) {
            editor.onDidChangeModelContent((e: monacoEditor.editor.IModelContentChangedEvent) => {
                if (e.changes.length === 1) {
                    let change = e.changes[0];

                    if (this.completionPosition &&
                        this.completionPosition.column === change.range.startColumn &&
                        this.completionPosition.lineNumber === change.range.startLineNumber &&
                        this.completionItems) {
                        let acceptedItem: ICompletionItem = this.completionItems.find((t) => t.insertText === change.text);
                        if (acceptedItem) {
                            this.props.client.acceptCompletionItem(acceptedItem);
                        }
                    }
                }
            });
        }

        this.setState({
            ...this.state,
            editorLanguage: this.props.editorLanguage,
            editor,
            setTheme: (t) => monacoModule.editor.setTheme(t),
            diagnosticsAdapter: new DiagnosticsAdapter(monacoModule, editor),
            instrumentationAdapter: new InstrumentationAdapter(editor),
            monacoModule: monacoModule
        });

        if (this.props.showCompletions) {
            let language = this.props.editorLanguage;
            let monaco = monacoModule;
            let capturedEditor = this;
            this.setupLanguageServices(monaco, language, capturedEditor);
        }

        window.addEventListener("resize", () => editor.layout());
        this.props.editorDidMount(editor, monacoModule);
        this.contentChangedChannel
            .pipe(debounce(() => interval(1000)))
            .subscribe((event) => this.textChangedEventHandler(event));
    }

    private setupLanguageServices = (monaco: typeof monacoEditor, language: SupportedLanguages, capturedEditor: Editor) => {
        monaco.languages.registerCompletionItemProvider(language,
            {
                provideCompletionItems: async function (
                    model: monacoEditor.editor.ITextModel,
                    position: monacoEditor.Position,
                    _token: monacoEditor.CancellationToken,
                    _context: monacoEditor.languages.CompletionContext) {
                    capturedEditor.completionPosition = position;
                    let client = capturedEditor.props.client;
                    let workspace = cloneWorkspace(capturedEditor.props.workspace);
                    let activeBuffer = capturedEditor.props.activeBufferId;
                    setBufferContent(workspace, activeBuffer, model.getValue());

                    let completionResult = await client.getCompletionList(
                        workspace,
                        activeBuffer,
                        model.getOffsetAt(position),
                        capturedEditor.props.completionProvider);
                    capturedEditor.completionItems = completionResult.items;
                    capturedEditor.setDiagnostics(completionResult.diagnostics);
                    return capturedEditor.completionItems;
                },
                triggerCharacters: ["."]
            });

        monaco.languages.registerSignatureHelpProvider(language,
            {
                provideSignatureHelp: async function (
                    model: monacoEditor.editor.ITextModel,
                    position: monacoEditor.Position,
                    _token: monacoEditor.CancellationToken) {
                    let client = capturedEditor.props.client;
                    let workspace = cloneWorkspace(capturedEditor.props.workspace);
                    let activeBuffer = capturedEditor.props.activeBufferId;
                    setBufferContent(workspace, activeBuffer, model.getValue());
                    let result = await client.getSignatureHelp(workspace, activeBuffer, model.getOffsetAt(position));
                    capturedEditor.setDiagnostics(result.diagnostics);
                    return result;
                },
                signatureHelpTriggerCharacters: ["("]
            }
        );
    }

    private defineThemes = (defineTheme: (name: string, themeData: monacoEditor.editor.IStandaloneThemeData) => void) => {
        if (this.props.themes) {
            for (var name in this.props.themes) {
                if (defineTheme === null) {
                    this.state.defineTheme(name, this.props.themes[name]);
                }
                else {
                    defineTheme(name, this.props.themes[name]);
                }
            }
        }
    }

    public componentDidUpdate(oldProps: IEditorProps) {
        if (this.props.editorOptions) {
            if (oldProps.editorOptions !== this.props.editorOptions) {
                if (this.state && this.state.editor) {
                    this.state.editor.updateOptions(this.props.editorOptions);
                }
            }
        }

        if (this.props.themes) {
            if (!oldProps.themes || !deepEqual(oldProps.themes, this.props.themes)) {
                if (this.state && this.state.defineTheme) {
                    this.defineThemes(null);
                }
            }
        }

        if (this.props.theme) {
            if (oldProps.theme !== this.props.theme) {
                if (this.state && this.state.setTheme) {

                    this.state.setTheme(this.props.theme);
                }
            }
        }

        let runState = this.props.runState;
        let compileState = this.props.compileState;
        let diagnostics: IDiagnostic[] = [];
        if (runState && runState.diagnostics && runState.diagnostics.length > 0) {
            diagnostics = runState.diagnostics;
        } else if (compileState && compileState.diagnostics && compileState.diagnostics.length > 0) {
            diagnostics = compileState.diagnostics;
        }

        this.setDiagnostics(diagnostics);
        const canShowInstrumentation = this.state
            && this.state.instrumentationAdapter
            && this.props.runState
            && this.props.runState.instrumentation;

        if (canShowInstrumentation) {
            this.state.instrumentationAdapter.setOrUpdateInstrumentation(oldProps, this.props, this.state);
        }

        const instrumentationEnabledFallingEdge =
            this.props && oldProps &&
            oldProps.instrumentationEnabled && !this.props.instrumentationEnabled;

        if (instrumentationEnabledFallingEdge) {
            this.state.instrumentationAdapter.clearInstrumentation();
        }

        if (this.props && oldProps && oldProps.workspace !== this.props.workspace) {
            this.queueDiagnosticsUpdateRequest();
        }

        if (this.state && (this.props.editorLanguage !== oldProps.editorLanguage)) {
            this.setState({
                ...this.state,
                editorLanguage: this.props.editorLanguage
            });

            if (this.state && this.state.monacoModule && this.props.showCompletions) {
                let language = this.props.editorLanguage;
                let monaco = this.state.monacoModule;
                let capturedEditor = this;
                this.setupLanguageServices(monaco, language, capturedEditor);
            }
        }
    }

    private setDiagnostics = ((diagnostics: IDiagnostic[]): void => {
        let shouldUpdate = false;
        if (this.state && this.state.diagnosticsAdapter) {
            if (diagnostics && diagnostics.length > 0) {
                shouldUpdate = this.state.diagnosticsAdapter.setDiagnostics(diagnostics);
            }
            else {
                shouldUpdate = this.state.diagnosticsAdapter.clearDiagnostics();
            }
        }
        if (shouldUpdate && this.props && this.props.updateDiagnostics) {
            this.props.updateDiagnostics(diagnostics);
        }
    }).bind(this);

    private onChange = ((newValue: any, _event: monacoEditor.editor.IModelContentChangedEvent) => {
        if (this.props.sourceCodeDidChange) {
            this.props.sourceCodeDidChange(newValue, this.props.activeBufferId);
        }

        if (this.state && this.state.instrumentationAdapter) {
            this.state.instrumentationAdapter.clearInstrumentation();
        }

        let offset = 0;

        if (this.state && this.state.editor) {
            let model = this.state.editor.getModel() as monacoEditor.editor.ITextModel;
            if (model) {
                let pos = this.state.editor.getPosition();
                offset = model.getOffsetAt(pos);
            }
        }

        this.contentChangedChannel.next({ text: newValue, position: offset });
        //this.setDiagnostics([]);
    }).bind(this);

    public render() {
        return this.props.showEditor ?
            (<ReactMonacoEditor
                editorDidMount={this.editorDidMount}
                editorWillMount={this.editorWillMount}
                language={this.props.editorLanguage}
                options={this.props.editorOptions}
                onChange={this.onChange}
                value={this.props.sourceCode}
            />
            ) :
            (<div></div>);
    }

    public static defaultProps: IEditorProps = {
        completionProvider: "",
        editorDidMount: (_editor: monacoEditor.editor.IEditor) => { },
        editorWillMount: (_editor: typeof monacoEditor) => { },
        editorOptions: { scrollBeyondLastLine: false },
        showEditor: true,
        sourceCodeDidChange: (_sourceCode: string) => { },
        client:
        {
            acceptCompletionItem: (_selection: ICompletionItem) => { throw Error(); },
            getWorkspaceFromGist: async (_gistId: string, _workspaceType: string, _extractBuffers: boolean) => { throw Error(); },
            getSourceCode: async (_request: IWorkspaceRequest) => { throw Error(); },
            run: async (_args: IRunRequest) => { throw Error(); },
            getCompletionList: async (_workspace: IWorkspace, _bufferId: string, _position: number, _completionProvider: string) => { throw Error(); },
            getSignatureHelp: async (_workspace: IWorkspace, _bufferId: string, _position: number) => { throw Error(); },
            compile: async (_args: IRunRequest) => { throw Error(); },
            createProjectFromGist: (_request: CreateProjectFromGistRequest) => { throw Error(); },
            createRegionsFromProjectFiles: (_request: CreateRegionsFromFilesRequest) => { throw Error(); },
            getDiagnostics: (_workspace: IWorkspace, _bufferId: string) => { throw Error(); },
            getConfiguration: () => { throw Error(); }
        },
        showCompletions: true,
        activeBufferId: "Program.cs",
        editorLanguage: "csharp"
    };
}

const mapStateToProps = (state: IState): IEditorProps => {
    var props: IEditorProps = {
        completionProvider: state.config.completionProvider,
        editorOptions: { ...state.monaco.editorOptions, scrollBeyondLastLine: false },
        showEditor: state.ui.showEditor,
        sourceCode: state.monaco.displayedCode,
        theme: state.monaco.theme,
        themes: state.monaco.themes,
        client: state.config.client,
        showCompletions: state.config.version >= 2,
        runState: state.run,
        compileState: state.compile,
        activeBufferId: state.monaco.bufferId,
        workspace: state.workspace.workspace,
        instrumentationEnabled: state.workspace.workspace.includeInstrumentation,
        editorLanguage: state.monaco.language
    };
    return props;
};

const mapDispatchToProps: MapDispatchToProps<IEditorProps, IEditorProps> = (dispatch: Dispatch, _ownProps: IEditorProps): IEditorProps => {
    return ({
        editorDidMount: (editor: monacoEditor.editor.IEditor) => dispatch(actions.notifyMonacoReady(editor)),
        sourceCodeDidChange: (sourceCode: string, bufferId: string) => {
            dispatch(actions.updateWorkspaceBuffer(sourceCode, bufferId));
        },
        updateDiagnostics: (diagnostics: IDiagnostic[]) => dispatch(actions.setDiagnostics(diagnostics))
    });
};

export default connect(mapStateToProps, mapDispatchToProps)(Editor);
