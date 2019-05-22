// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TextChangedEvent, Theme, MonacoEditorOptions, MonacoEditorConfiguration } from "../../src";
import { Observable, Subject } from "rxjs";
import { ITrydotnetMonacoTextEditor } from "../../src/internals/monacoTextEditor";

export class FakeMonacoTextEditor implements ITrydotnetMonacoTextEditor {

    textChanges: Observable<TextChangedEvent> = new Subject<TextChangedEvent>();
    content: string;
    private currentbufferId: string;
    theme: Theme;
    options: MonacoEditorOptions;

    constructor(private editorId: string) {
        if (!this.editorId) {
            this.editorId = "0";
        }
    }

    public id(): string {
        return this.editorId;
    }

    public async setBufferId(bufferId: string): Promise<void> {
        this.currentbufferId = bufferId;
        return Promise.resolve();
    }

    public async setContent(content: string): Promise<void> {
        this.content = content;
        return Promise.resolve();
    }

    public raiseTextEvent(content: string) {
        (<Subject<TextChangedEvent>>this.textChanges).next({
            text: content,
            documentId: this.currentbufferId,
        });
    }

    public setTheme(theme: Theme): void {
        this.theme = theme;
    }
    public setOptions(options: MonacoEditorOptions): void {
        this.options = options;
    }

    public configure(configuration: MonacoEditorConfiguration): void {
        this.setOptions(configuration.options);
        this.setTheme(configuration.theme);
    }
}