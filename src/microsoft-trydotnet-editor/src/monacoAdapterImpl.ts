// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';
import { Logger } from '@microsoft/dotnet-interactive';
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

        this._onDidChangeModelContentEvents.pipe(rxjs.debounce(() => rxjs.interval(1000))).subscribe({
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
