// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TextChangedEvent, IMonacoEditor, Theme, MonacoEditorTheme, MonacoEditorOptions, MonacoEditorConfiguration } from "../editor";
import { Observable, Subject } from "rxjs";
import { IMessageBus } from "./messageBus";
import { CODE_CHANGED_EVENT, SET_EDITOR_CODE_REQUEST, SET_ACTIVE_BUFFER_REQUEST, CONFIGURE_MONACO_REQUEST, DEFINE_THEMES_REQUEST, ApiMessage } from "./apiMessages";
import { IRequestIdGenerator } from "./requestIdGenerator";
import { isNullOrUndefinedOrWhitespace } from "../stringExtensions";

export interface ITrydotnetMonacoTextEditor extends IMonacoEditor {
    setBufferId(bufferId: string): Promise<void>;
}

export class MonacoTextEditor implements ITrydotnetMonacoTextEditor {
    textChanges: Observable<TextChangedEvent>;
    private currentbufferId: string;
    constructor(private editorApimessageBus: IMessageBus, private requestIdGenerator: IRequestIdGenerator, private editorId: string) {
        if (!this.editorApimessageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }

        this.textChanges = new Subject<TextChangedEvent>();

        let codeChangedHandler = ((message: ApiMessage) => {
            if (message && message.type === CODE_CHANGED_EVENT) {
                let event = {
                    text: message.sourceCode,
                    documentId: message.bufferId,
                    editorId: this.editorId
                };
                (<Subject<TextChangedEvent>>this.textChanges).next(event);
            }
        }).bind(this);
        this.editorApimessageBus.subscribe(codeChangedHandler);
    }

    public id(): string {
        return this.editorId;
    }

    public messageBus() {
        return this.editorApimessageBus;
    }

    public async setBufferId(bufferId: string): Promise<void> {
        this.currentbufferId = bufferId;
        let requestId = await this.requestIdGenerator.getNewRequestId();
        this.editorApimessageBus.post({
            type: SET_ACTIVE_BUFFER_REQUEST,
            requestId: requestId,
            bufferId: this.currentbufferId
        });
    }

    public async setContent(content: string): Promise<void> {
        let requestId = await this.requestIdGenerator.getNewRequestId();
        this.editorApimessageBus.post({
            type: SET_EDITOR_CODE_REQUEST,
            requestId: requestId,
            sourceCode: content
        });
    }

    public setTheme(theme: Theme): void {
        if (theme) {
            if (isThemeObject(theme)) {
                this._defineTheme(theme.name, theme.monacoEditorTheme);
                this._setTheme(theme.name);
            } else if (!isNullOrUndefinedOrWhitespace(<string>theme)) {
                this._setTheme(theme);
            }
        }
    };

    public setOptions(options: MonacoEditorOptions): void {

        this.editorApimessageBus.post({
            type: CONFIGURE_MONACO_REQUEST,
            editorOptions: { ...options }
        });
    }

    public configure(configuration: MonacoEditorConfiguration): void {
        this.setOptions(configuration.options);
        this.setTheme(configuration.theme);
    }

    private _defineTheme(themeName: string, editorTheme: any): void {
        let name = isNullOrUndefinedOrWhitespace(themeName) ? "trydotnetJs" : themeName;
        this.editorApimessageBus.post({
            type: DEFINE_THEMES_REQUEST,
            themes: {
                [name]: { ...editorTheme }
            }
        });
    }

    private _setTheme(themeName: string): void {
        this.editorApimessageBus.post({
            type: CONFIGURE_MONACO_REQUEST,
            theme: themeName
        });
    }
}

function isThemeObject(theme: Theme): theme is MonacoEditorTheme {
    return (<MonacoEditorTheme>theme).monacoEditorTheme !== undefined;
}