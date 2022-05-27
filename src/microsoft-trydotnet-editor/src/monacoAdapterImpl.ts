// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DiagnosticsProduced, Logger } from '@microsoft/dotnet-interactive';
import * as monaco from 'monaco-editor';
import * as rxjs from 'rxjs';
import * as editorAdapter from './EditorAdapter';

export class MonacoEditorAdapter extends editorAdapter.EditorAdapter {
    protected displayDiagnostics(diagnostics: DiagnosticsProduced) {
        let markers: monaco.editor.IMarkerData[] = [];
        const model = this._editor.getModel();
        for (const diagnostic of diagnostics.diagnostics) {
            let severity = monaco.MarkerSeverity.Info;

            switch (diagnostic.severity) {
                case 'error':
                    severity = monaco.MarkerSeverity.Error;
                    break;
                case 'warning':
                    severity = monaco.MarkerSeverity.Warning;
                    break;
                case 'info':
                    severity = monaco.MarkerSeverity.Info;
                    break;

            }

            markers.push({
                message: diagnostic.message,
                severity: diagnostic.severity === 'error' ? monaco.MarkerSeverity.Error : monaco.MarkerSeverity.Warning,
                startLineNumber: diagnostic.linePositionSpan.start.line,
                startColumn: diagnostic.linePositionSpan.start.character,
                endLineNumber: diagnostic.linePositionSpan.end.line,
                endColumn: diagnostic.linePositionSpan.end.character

            });
        }

        monaco.editor.setModelMarkers(model, "trydotnetdiagnostics", markers);

        throw new Error('Method not implemented.');
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

    private _onDidChangeModelContenEvents: rxjs.Subject<monaco.editor.IModelContentChangedEvent> = new rxjs.Subject<monaco.editor.IModelContentChangedEvent>();

    constructor(private _editor: monaco.editor.IStandaloneCodeEditor) {
        super();

        this._editor.onDidChangeModelContent(e => {
            this._onDidChangeModelContenEvents.next(e);
        });

        this._onDidChangeModelContenEvents.pipe(rxjs.debounce(() => rxjs.interval(1000))).subscribe({
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
        Logger.default.info(`[MonacoEditorArapter.getCode]: ${code}`);
        return code;
    }

    setCode(code: string) {
        Logger.default.info(`[MonacoEditorArapter.setCode]: ${code}`);
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

        // monaco.languages.registerCompletionItemProvider(language, {
        //     provideCompletionItems: (model: monaco.editor.ITextModel, position: monaco.Position, context: monaco.languages.CompletionContext, token: monaco.CancellationToken) => {
        //         throw new Error('Method not implemented.');
        //     }
        // });

        // monaco.languages.registerHoverProvider(language, {
        //     provideHover: (model: monaco.editor.ITextModel, position: monaco.Position, token: monaco.CancellationToken) => {
        //         throw new Error('Method not implemented.');
        //     }
        // });

        // monaco.languages.registerSignatureHelpProvider(language, {
        //     provideSignatureHelp: (model: monaco.editor.ITextModel, position: monaco.Position, token: monaco.CancellationToken, context: monaco.languages.SignatureHelpContext) => {
        //         throw new Error('Method not implemented.');
        //     }
        // });
    }

    defineTheme(themes: { [x: string]: monaco.editor.IStandaloneThemeData }) {
        if (monaco.editor.defineTheme) {
            for (const key in themes) {
                monaco.editor.defineTheme(key, themes[key]);
            }
        }
    }
}
