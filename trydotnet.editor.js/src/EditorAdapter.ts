// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';
import * as rxjs from 'rxjs';

export abstract class EditorAdapter {
    abstract getLanguage(): string | undefined;
    public abstract setLanguage(language: string): void;
    abstract defineTheme(themes: { [x: string]: any }): void;
    abstract getCode(): string;
    abstract setCode(code: string): void;
    abstract getPosition(): { line: number, column: number };
    abstract setPosition(position: { line: number, column: number }): void;
    abstract updateOptions(options: any): void;

    protected abstract displayDiagnostics(diagnostics: dotnetInteractive.DiagnosticsProduced);

    private _kernel: dotnetInteractive.Kernel;
    private _editorChanges: rxjs.Subject<ContentChangedEvent> = new rxjs.Subject<ContentChangedEvent>();

    constructor() {
        const handler = this.handleContentChanged.bind(this);
        this._editorChanges.subscribe({
            next: handler
        });

    }

    private handleContentChanged(contentChanged: ContentChangedEvent) {
        if (this._kernel) {
            this._kernel.send({
                commandType: dotnetInteractive.RequestDiagnosticsType,
                command: <dotnetInteractive.RequestDiagnostics>{
                    code: contentChanged.code
                }
            });
        }
    }


    public get editorChanges(): rxjs.Observable<ContentChangedEvent> {
        return this._editorChanges;
    }

    protected get kernel(): dotnetInteractive.Kernel {
        return this._kernel;
    }

    protected publishContentChanged(contentChanged: ContentChangedEvent) {
        this._editorChanges.next(contentChanged);
    }

    configureServices(kernel: dotnetInteractive.Kernel) {
        this._kernel = kernel;
        if (!this._kernel) {
            throw new Error('kernel is null');
        }

        const eventHandler = this.handleKernelEvent.bind(this);
        this.kernel.subscribeToKernelEvents((eventEnvelope) => {
            eventHandler(eventEnvelope);
        });
    }

    private handleKernelEvent(eventEnvelope: dotnetInteractive.KernelEventEnvelope) {
        switch (<any>eventEnvelope.eventType) {

            case dotnetInteractive.DocumentOpenedType:
                const DocumentOpened = <dotnetInteractive.DocumentOpened>eventEnvelope.event;
                this.setCode(DocumentOpened.content);
                break;
            case dotnetInteractive.DiagnosticsProducedType:
                this.displayDiagnostics(<dotnetInteractive.DiagnosticsProduced>eventEnvelope.event);
                break;
        }
    }

}

export interface ContentChangedEvent {
    code: string;
    position: {
        line: number;
        column: number;
    }
};
