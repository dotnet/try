// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TextChangedEvent, Theme, MonacoEditorOptions, MonacoEditorConfiguration } from "../../src";
import { Observable, Subject } from "rxjs";
import { ITrydotnetMonacoTextEditor } from "../../src/internals/monacoTextEditor";
import { DocumentId } from "../../src/documentId";
export class FakeMonacoTextEditor implements ITrydotnetMonacoTextEditor {

    textChanges: Observable<TextChangedEvent> = new Subject<TextChangedEvent>();
    content: string;
    private currentbufferId: DocumentId;
    theme: Theme;
    options: MonacoEditorOptions;

    constructor(private editorId: string) {
        if (!this.editorId) {
            this.editorId = "0";
        }
    }
    setSize(size: { width: number; height: number; }): void {

    }

    public id(): string {
        return this.editorId;
    }

    public async setBufferId(bufferId: DocumentId): Promise<void> {
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
        if (configuration.options) {
            this.setOptions(configuration.options);
        }
        if (configuration.theme) {
            this.setTheme(configuration.theme);
        }
    }
}