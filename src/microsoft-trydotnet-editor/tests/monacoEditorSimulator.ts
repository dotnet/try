// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as editorAdapter from "../src/EditorAdapter";

export class MonacoEditorSimulator extends editorAdapter.EditorAdapter {

    private _language: string;
    private _themes: { [x: string]: any; } = {};
    private _position: {
        line: number;
        column: number;
    } = { line: 1, column: 1 };

    private _options = {};
    private _code: string;
    private _size?: { width: number; height: number; }

    private _markers: editorAdapter.IMarkerData[] = [];

    layout(size: { width: number; height: number; } | undefined): void {
        this._size = size;
    }

    setMarkers(markers: editorAdapter.IMarkerData[]) {
        this._markers = markers;
    }

    getMarkers(): editorAdapter.IMarkerData[] {
        return this._markers;
    }

    getPosition(): { line: number; column: number; } {
        return { ...this._position };
    }

    setPosition(position: { line: number; column: number; }): void {
        this._position = { ...position }; //?
    }

    getLanguage(): string {
        return this._language;
    }

    public setLanguage(language: string) {
        this._language = language;
    }

    defineTheme(themes: { [x: string]: any; }) {
        this._themes = { ... this._themes, ...themes };
    }

    getCode(): string {
        return this._code; //?
    }

    setCode(code: string) {
        this._code = code; //? 
        this.publishContentChanged({ code: this._code, position: { ...this._position } });

    }

    updateOptions(options: any) {
        this._options = { ... this._options, ...options };//?
    }

    focus(): void {

    }

    public type(text: string) {
        let newState = modifyText(this._code ?? "", this._position ?? { line: 1, column: 1 }, text ?? "");//?

        this._code = newState.newText;//? 
        this._position = { line: newState.line, column: newState.column };//?

        this.publishContentChanged({ code: this._code, position: { ...this._position } });
    }
}

export function modifyText(original: string, currentPosition: { line: number; column: number; }, text: string): { newText: string, line: number; column: number; } {
    let editStartAboslutePosition = 0;
    let originalLine = currentPosition.line;

    for (let linePos = 1; linePos < originalLine; linePos++) {
        let newLinePosition = original.indexOf("\n", editStartAboslutePosition);
        if (newLinePosition === -1) {
            break;
        }
        editStartAboslutePosition = original.indexOf("\n", editStartAboslutePosition) + 1;
    }

    let originalColumn = Math.min(currentPosition.column - 1, original.slice(editStartAboslutePosition).length);


    editStartAboslutePosition += originalColumn;

    originalColumn += 1;

    let newText = original.slice(0, editStartAboslutePosition) + text + original.slice(editStartAboslutePosition);


    let line = 1;

    let editEndAbsolutePosition = editStartAboslutePosition + text.length;
    let newTextLastCharacter = text[text.length - 1];
    let lastNewLineAbsolutePosition = 0;

    console.log(`newText : ${newText}`);
    console.log(`compute new position`);

    for (let i = 0; i < editEndAbsolutePosition;) {
        i = newText.indexOf("\n", i);
        console.log(`last new line position : ${i}`);
        if (i === -1) {
            break;
        }
        else if (i <= editEndAbsolutePosition) {
            line++;
            lastNewLineAbsolutePosition = i;
            i++;
        }
        else {
            break;
        }

    }

    console.log(`lastNewLineAbsolutePosition: ${lastNewLineAbsolutePosition}`);
    let newTextLastCharacterAbsolutePosition = 0;

    for (let i = lastNewLineAbsolutePosition; i < editEndAbsolutePosition;) {
        i = newText.indexOf(newTextLastCharacter, i);
        if (i === -1) {
            break;
        }
        else if (i <= editEndAbsolutePosition) {
            newTextLastCharacterAbsolutePosition = i;
            i++;
        }
        else {
            break;
        }
    }
    console.log(`newTextLastCharacterAbsolutePosition: ${newTextLastCharacterAbsolutePosition}`);

    let newLineStart = lastNewLineAbsolutePosition > 0 ? lastNewLineAbsolutePosition + 1 : 0;
    let column = Math.max(1, newText.slice(newLineStart, newTextLastCharacterAbsolutePosition + 1).length + 1);
    console.log(newText[lastNewLineAbsolutePosition]);
    console.log(newText[newTextLastCharacterAbsolutePosition]);

    return { newText, line: line, column: column };
}