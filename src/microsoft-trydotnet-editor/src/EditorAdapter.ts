// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';
import * as rxjs from 'rxjs';
import { DebouncingKernel } from './debouncingKernel';

export enum MarkerSeverity {
    Hint = 1,
    Info = 2,
    Warning = 4,
    Error = 8
}

export interface IMarkerData {
    severity: MarkerSeverity;
    message: string;
    startLineNumber: number;
    startColumn: number;
    endLineNumber: number;
    endColumn: number;
}

export abstract class EditorAdapter {

    private _languageServiceEnabled: boolean;
    private _textChangedEventsEnabled: boolean;
    abstract getLanguage(): string | undefined;
    public abstract setLanguage(language: string): void;
    abstract defineTheme(themes: { [x: string]: any }): void;
    abstract getCode(): string;
    abstract setCode(code: string): void;
    abstract getPosition(): { line: number, column: number };
    abstract setPosition(position: { line: number, column: number }): void;
    abstract updateOptions(options: any): void;
    abstract focus(): void;
    abstract layout(size: { width: number, height: number }): void;


    private _diagnostics: polyglotNotebooks.Diagnostic[] = [];
    abstract setMarkers(markers: IMarkerData[]);

    displayDiagnostics(diagnostics: polyglotNotebooks.Diagnostic[]) {
        const markers: IMarkerData[] = [];

        for (const diagnostic of diagnostics) {
            let severity = MarkerSeverity.Info;

            if (diagnostic.severity === 'hidden') {
                continue;
            }

            switch (diagnostic.severity) {
                case 'error':
                    severity = MarkerSeverity.Error;
                    break;
                case 'warning':
                    severity = MarkerSeverity.Warning;
                    break;
                case 'info':
                    severity = MarkerSeverity.Info;
                    break;
            }

            // interactive diagnostics are 0-based, monaco is 1-based
            markers.push({
                message: diagnostic.message,
                severity: severity,
                startLineNumber: diagnostic.linePositionSpan.start.line + 1,
                startColumn: diagnostic.linePositionSpan.start.character + 1,
                endLineNumber: diagnostic.linePositionSpan.end.line + 1,
                endColumn: diagnostic.linePositionSpan.end.character + 1
            });
        }

        this.setMarkers(markers);
    }

    abstract getMarkers(): IMarkerData[];

    private _kernel: DebouncingKernel;
    private _editorChanges: rxjs.Subject<ContentChangedEvent> = new rxjs.Subject<ContentChangedEvent>();

    constructor() {
        const handler = this.handleContentChanged.bind(this);
        // debounce editor events
        this._editorChanges.pipe(rxjs.debounceTime(500)).subscribe({
            next: handler
        });

        this._languageServiceEnabled = true;
        this._textChangedEventsEnabled = true;
    }

    protected get languageServiceEnabled(): boolean {
        return this._languageServiceEnabled;
    }

    public get diagnostics(): polyglotNotebooks.Diagnostic[] {
        return this._diagnostics;
    }

    enableLanguageService() {
        this._languageServiceEnabled = true;
    }
    disableLanguageService() {
        this._languageServiceEnabled = false;
    }

    enableTextChangedEvents() {
        this._textChangedEventsEnabled = true;
    }
    disableTextChangedEvents() {
        this._textChangedEventsEnabled = false;
    }

    private handleContentChanged(contentChanged: ContentChangedEvent) {
        if (this._kernel && this._languageServiceEnabled) {
            this._kernel.send(new polyglotNotebooks.KernelCommandEnvelope(
                polyglotNotebooks.RequestDiagnosticsType,
                <polyglotNotebooks.RequestDiagnostics>{
                    code: contentChanged.code
                }
            ));
        }
    }


    public get editorChanges(): rxjs.Observable<ContentChangedEvent> {
        return this._editorChanges;
    }

    protected get kernel(): DebouncingKernel {
        return this._kernel;
    }

    protected publishContentChanged(contentChanged: ContentChangedEvent) {
        if (this._textChangedEventsEnabled) {
            this._editorChanges.next(contentChanged);
        }
    }

    configureServices(kernel: DebouncingKernel) {
        this._kernel = kernel;
        if (!this._kernel) {
            throw new Error('kernel is null');
        }

        const eventHandler = this.handleKernelEvent.bind(this);
        this.kernel.subscribeToKernelEvents((eventEnvelope) => {
            eventHandler(eventEnvelope);
        });
    }

    private handleKernelEvent(eventEnvelope: polyglotNotebooks.KernelEventEnvelope) {
        switch (<any>eventEnvelope.eventType) {
            case polyglotNotebooks.DiagnosticsProducedType:
                const diagnosticsEvent = <polyglotNotebooks.DiagnosticsProduced>eventEnvelope.event;
                this._diagnostics = diagnosticsEvent.diagnostics;
                this.displayDiagnostics(diagnosticsEvent.diagnostics);
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
