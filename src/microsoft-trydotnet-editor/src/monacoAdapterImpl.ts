// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';
import { Logger, submitCommandAndGetResult } from '@microsoft/dotnet-interactive';
import * as monaco from 'monaco-editor';
import * as rxjs from 'rxjs';
import * as editorAdapter from './EditorAdapter';

const markerOwnerName: string = 'trydotnetdiagnostics';

export class MonacoEditorAdapter extends editorAdapter.EditorAdapter {
    setMarkers(markers: editorAdapter.IMarkerData[]) {
        const model = this._editor.getModel();
        monaco.editor.setModelMarkers(model, markerOwnerName, markers);
        Logger.default.info('[MonacoEditorAdapter.setMarkers]: ' + JSON.stringify(markers));
    }

    getMarkers(): editorAdapter.IMarkerData[] {
        const markers = monaco.editor.getModelMarkers({ owner: markerOwnerName });
        return markers;
    }

    getPosition(): { line: number; column: number; } {
        const position = this._editor.getPosition() ?? { lineNumber: 1, column: 1 };

        return {
            line: position.lineNumber,
            column: position.column
        };
    }

    setPosition(position: { line: number; column: number; }): void {
        this._editor.setPosition({ lineNumber: position.line, column: position.column });
    }

    focus(): void {
        this._editor.focus();
    }

    private _onDidChangeModelContentEvents: rxjs.Subject<monaco.editor.IModelContentChangedEvent> = new rxjs.Subject<monaco.editor.IModelContentChangedEvent>();

    constructor(private _editor: monaco.editor.IStandaloneCodeEditor) {
        super();

        this._editor.onDidChangeModelContent(e => {
            this._onDidChangeModelContentEvents.next(e);
        });

        this._onDidChangeModelContentEvents.pipe(rxjs.debounce(() => rxjs.interval(500))).subscribe({
            next: async (_contentChanged) => {
                const code = this._editor.getValue();
                const position = this._editor.getPosition() ?? { lineNumber: 1, column: 1 };
                const contentChangedEvent: editorAdapter.ContentChangedEvent = {
                    code: code,
                    position: {
                        line: position.lineNumber,
                        column: position.column
                    }
                };
                this.publishContentChanged(contentChangedEvent);
            }
        });
    }

    getCode(): string {
        const code = this._editor.getValue();
        dotnetInteractive.Logger.default.info(`[MonacoEditorArapter.getCode]: ${code}`);
        return code;
    }

    setCode(code: string) {
        dotnetInteractive.Logger.default.info(`[MonacoEditorArapter.setCode]: ${code}`);
        this._editor.setValue(code);
    }

    updateOptions(options: any) {
        this._editor.updateOptions(options);
        this._editor.layout();
    }

    getLanguage(): string {
        return this._editor.getModel().getLanguageId();
    }

    public setLanguage(language: string) {
        monaco.editor.setModelLanguage(this._editor.getModel(), language);

        monaco.languages.registerCompletionItemProvider(language, {
            triggerCharacters: ['.'],
            provideCompletionItems: async (model: monaco.editor.ITextModel, position: monaco.Position, context: monaco.languages.CompletionContext, token: monaco.CancellationToken) => {
                if (this.languageServiceEnabled) {
                    const command: dotnetInteractive.RequestCompletions = {
                        code: this.getCode(),
                        linePosition: {
                            line: position.lineNumber - 1,
                            character: position.column - 1,
                        }
                    };
                    const commandEnvelope: dotnetInteractive.KernelCommandEnvelope = {
                        commandType: dotnetInteractive.RequestCompletionsType,
                        command
                    };
                    const completionsProduced = await submitCommandAndGetResult<dotnetInteractive.CompletionsProduced>(this.kernel, commandEnvelope, dotnetInteractive.CompletionsProducedType);
                    const completionList: monaco.languages.CompletionList = {
                        suggestions: completionsProduced.completions.map(completion => ({
                            label: completion.displayText,
                            kind: mapToCompletionItemKind(completion.kind),
                            insertText: completion.insertText,
                            documentation: completion.documentation,
                            range: {
                                startLineNumber: position.lineNumber,
                                startColumn: position.column,
                                endLineNumber: position.lineNumber,
                                endColumn: position.column,
                            }
                        })),
                    };
                    return completionList;
                } else {
                    return { suggestions: [] };
                }
            }
        });

        monaco.languages.registerSignatureHelpProvider(language, {
            signatureHelpTriggerCharacters: ['(', ','],
            provideSignatureHelp: async (model: monaco.editor.ITextModel, position: monaco.Position, token: monaco.CancellationToken, context: monaco.languages.SignatureHelpContext) => {
                if (this.languageServiceEnabled) {
                    const command: dotnetInteractive.RequestSignatureHelp = {
                        code: this.getCode(),
                        linePosition: {
                            line: position.lineNumber - 1,
                            character: position.column - 1,
                        }
                    };
                    const commandEnvelope: dotnetInteractive.KernelCommandEnvelope = {
                        commandType: dotnetInteractive.RequestSignatureHelpType,
                        command
                    };
                    const signatureHelpProduced = await submitCommandAndGetResult<dotnetInteractive.SignatureHelpProduced>(this.kernel, commandEnvelope, dotnetInteractive.SignatureHelpProducedType);
                    const signatureHelp: monaco.languages.SignatureHelp = {
                        signatures: signatureHelpProduced.signatures,
                        activeSignature: signatureHelpProduced.activeSignatureIndex,
                        activeParameter: signatureHelpProduced.activeParameterIndex,
                    };
                    const signatureHelpResult: monaco.languages.SignatureHelpResult = {
                        value: signatureHelp,
                        dispose: () => { },
                    };
                    return signatureHelpResult;
                } else {
                    return { value: undefined, dispose: () => { } };
                }
            }
        });
    }

    defineTheme(themes: { [x: string]: monaco.editor.IStandaloneThemeData }) {
        if (monaco.editor.defineTheme) {
            for (const key in themes) {
                monaco.editor.defineTheme(key, themes[key]);
            }
        }
    }
}

function mapToCompletionItemKind(kind: string): monaco.languages.CompletionItemKind {
    switch (kind) {
        case 'Class':
            return monaco.languages.CompletionItemKind.Class;
        case 'Constant':
            return monaco.languages.CompletionItemKind.Constant;
        case 'Delegate':
            return monaco.languages.CompletionItemKind.Function;
        case 'Enum':
            return monaco.languages.CompletionItemKind.Enum;
        case 'EnumMember':
            return monaco.languages.CompletionItemKind.EnumMember;
        case 'Event':
            return monaco.languages.CompletionItemKind.Event;
        case 'ExtensionMethod':
            return monaco.languages.CompletionItemKind.Method;
        case 'Field':
            return monaco.languages.CompletionItemKind.Field;
        case 'Interface':
            return monaco.languages.CompletionItemKind.Interface;
        case 'Intrinsic':
            return monaco.languages.CompletionItemKind.Keyword;
        case 'Keyword':
            return monaco.languages.CompletionItemKind.Keyword;
        case 'Label':
            return monaco.languages.CompletionItemKind.Keyword;
        case 'Local':
            return monaco.languages.CompletionItemKind.Variable;
        case 'Method':
            return monaco.languages.CompletionItemKind.Method;
        case 'Module':
            return monaco.languages.CompletionItemKind.Module;
        case 'Namespace':
            return monaco.languages.CompletionItemKind.Module;
        case 'Operator':
            return monaco.languages.CompletionItemKind.Operator;
        case 'Parameter':
            return monaco.languages.CompletionItemKind.Variable;
        case 'Property':
            return monaco.languages.CompletionItemKind.Property;
        case 'RangeVariable':
            return monaco.languages.CompletionItemKind.Variable;
        case 'Reference':
            return monaco.languages.CompletionItemKind.Reference;
        case 'Structure':
            return monaco.languages.CompletionItemKind.Struct;
        case 'TypeParameter':
            return monaco.languages.CompletionItemKind.TypeParameter;

        default:
            Logger.default.warn(`[MonacoEditorAdapter.mapToCompletionItemKind] Unhandled completion item kind: ${kind}`);
            return monaco.languages.CompletionItemKind.Variable;
    }
}
