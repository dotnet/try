// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { TextChangedEvent, IMonacoEditor, Theme, MonacoEditorTheme, MonacoEditorOptions, MonacoEditorConfiguration } from "../editor";
import { Observable, Subject } from "rxjs";
import { IMessageBus } from "./messageBus";
import { ApiMessage } from "../apiMessages";
import { IRequestIdGenerator } from "./requestIdGenerator";
import { isNullOrUndefinedOrWhitespace } from "../stringExtensions";
import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";
import * as newContract from "../newContract";
import { DocumentId } from "../documentId";

export interface ITrydotnetMonacoTextEditor extends IMonacoEditor {
    setBufferId(bufferId: DocumentId): Promise<void>;
}

export class MonacoTextEditor implements ITrydotnetMonacoTextEditor {
    textChanges: Observable<TextChangedEvent>;
    private currentbufferId: DocumentId;
    constructor(private editorApimessageBus: IMessageBus, private requestIdGenerator: IRequestIdGenerator) {
        if (!this.editorApimessageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }

        this.textChanges = new Subject<TextChangedEvent>();

        let codeChangedHandler = ((message: ApiMessage) => {

            const m = <{ type: string }>message;
            if (m && m.type === newContract.EditorContentChangedType) {
                polyglotNotebooks.Logger.default.info(`[MonacoTextEditor.codeChangedHandler] ${JSON.stringify(m)}`);
                message;//?
                const editorContentChanged = <newContract.EditorContentChanged>m;
                const documentId = new DocumentId({ relativeFilePath: editorContentChanged.relativeFilePath, regionName: editorContentChanged.regionName });

                if (DocumentId.areEqual(documentId, this.currentbufferId)) {
                    polyglotNotebooks.Logger.default.info(`[MonacoTextEditor.codeChangedHandler] handling`);
                    let event = {
                        text: editorContentChanged.content,
                        documentId: this.currentbufferId
                    }; //?

                    (<Subject<TextChangedEvent>>this.textChanges).next(event);
                }
            }
        }).bind(this);
        this.editorApimessageBus.subscribe({
            next: (event) => codeChangedHandler(event)
        });
    }

    public id(): string {
        return "-0-";
    }

    public messageBus() {
        return this.editorApimessageBus;
    }

    public async setBufferId(bufferId: DocumentId): Promise<void> {
        this.currentbufferId = bufferId;
        let requestId = await this.requestIdGenerator.getNewRequestId();

        this.editorApimessageBus.post(<any>{
            type: polyglotNotebooks.OpenDocumentType,
            requestId: requestId,
            relativeFilePath: this.currentbufferId.relativeFilePath,
            regionName: this.currentbufferId.regionName
        });
    }

    public async setContent(content: string): Promise<void> {
        let requestId = await this.requestIdGenerator.getNewRequestId();
        const request: newContract.SetEditorContent = {
            type: newContract.SetEditorContentType,
            requestId: requestId,
            content: content
        }
        this.editorApimessageBus.post(request);
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
        const request: newContract.CongureMonacoEditor = {
            type: newContract.ConfigureMonacoEditorType,
            editorOptions: { ...options }
        };

        this.editorApimessageBus.post(request);
    }

    public setSize(size: { width: number, height: number }): void {
        const request: newContract.SetMonacoEditorSize = {
            type: newContract.SetMonacoEditorSizeType,
            size: size
        };

        this.editorApimessageBus.post(request);
    }

    public configure(configuration: MonacoEditorConfiguration): void {
        this.setOptions(configuration.options);
        this.setTheme(configuration.theme);
    }

    private _defineTheme(themeName: string, editorTheme: any): void {
        let name = isNullOrUndefinedOrWhitespace(themeName) ? "trydotnetJs" : themeName;
        const request: newContract.DefineMonacoEditorThemes = {
            type: newContract.DefineMonacoEditorThemesType,
            themes: {
                [name]: { ...editorTheme }
            }
        };
        this.editorApimessageBus.post(request);
    }

    private _setTheme(themeName: string): void {
        const request: newContract.CongureMonacoEditor = {
            type: newContract.ConfigureMonacoEditorType,
            theme: themeName
        };

        this.editorApimessageBus.post(request);
    }
}

function isThemeObject(theme: Theme): theme is MonacoEditorTheme {
    return (<MonacoEditorTheme>theme).monacoEditorTheme !== undefined;
}