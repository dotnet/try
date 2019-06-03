// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ITextDisplay } from "./textDisplay";
import { Observable } from "rxjs";

export type TextChangedEvent = {
    text: string,
    documentId: string,
    cursor?: number,
    editorId?: string
};

export type Theme = string | MonacoEditorTheme;

export type MonacoEditorTheme =
    {
        name: string,
        monacoEditorTheme: {}
    }


export type MonacoEditorConfiguration = {
    theme?: Theme,
    options?: MonacoEditorOptions
};

export type MonacoEditorOptions = {

}

export interface ITextEditor extends ITextDisplay {
    textChanges: Observable<TextChangedEvent>;
}

export interface IMonacoEditor extends ITextEditor {
    setTheme(theme: Theme): void;
    setOptions(options: MonacoEditorOptions): void;
    configure(configuration: MonacoEditorConfiguration): void;
}