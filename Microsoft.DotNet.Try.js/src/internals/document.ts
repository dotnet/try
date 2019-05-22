// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDocument } from "../editableDocument";
import { Subscription } from "rxjs";
import { ITrydotnetMonacoTextEditor } from "./monacoTextEditor";
import { ITextEditor, TextChangedEvent } from "../editor";
import { isNullOrUndefined } from "../stringExtensions";


export class Document implements IDocument {

    private editorSubscription: Subscription;
    private editor: ITrydotnetMonacoTextEditor;
    private cursorPosition: number = 0;

    public isModified: boolean = false;
    constructor(private documentId: string, private content: string) {
        if (!this.documentId) {
            throw new Error("documentId cannot be null");
        }
    }

    id(): string {
        return this.documentId;
    }

    getContent(): string {
        return this.content;
    }

    getCursorPosition(): number {
        return this.cursorPosition;
    }

    public currentEditor(): ITextEditor {
        return this.editor;
    }
    public isActiveInEditor(): boolean {
        return this.editor ? true : false;
    }
    async setContent(content: string): Promise<void> {
        if (this.content !== content) {
            this.isModified = true;
            this.content = content;
            if (this.editor) {
                await this.editor.setContent(content);
            }
        }
    }

    async bindToEditor(editor: ITrydotnetMonacoTextEditor): Promise<void> {
        this.unbind();
        if (editor) {
            this.editor = editor;
            await this.editor.setBufferId(this.documentId);
            await this.editor.setContent(this.content);
            let handler = ((event: TextChangedEvent) => {
                if (event.documentId === this.documentId) {
                    if(isNullOrUndefined(event.editorId) ||event.editorId === this.editor.id())
                    {
                        this.content = event.text;
                        this.cursorPosition = event.cursor ? event.cursor : 0;
                    }
                }
            }).bind(this);

            let onComplete = (() => this.unbind()).bind(this);

            this.editorSubscription = editor.textChanges.subscribe(
                event => handler(event),
                undefined,
                () => onComplete()
            );
        }
    }

    unbindFromEditor(): Promise<void> {
        this.unbind();
        this.cursorPosition = 0;
        return Promise.resolve();
    }

    private unbind() {
        if (this.editor) {
            this.editor = null;
        }

        if (this.editorSubscription) {
            this.editorSubscription.unsubscribe();
            this.editorSubscription = null;
        }
    }
}