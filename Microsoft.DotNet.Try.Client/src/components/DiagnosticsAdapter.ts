// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IDiagnostic } from "../IState";
import * as monacoEditor from "monaco-editor";
const deepEqual = require("deep-equal");

export default class DiagnosticsAdapter {
    private monacoModule: typeof monacoEditor;
    private editor: monacoEditor.editor.ICodeEditor;
    private currentMarkerData: monacoEditor.editor.IMarkerData[] = [];

    constructor(m: typeof monacoEditor, ed: monacoEditor.editor.ICodeEditor) {
        this.monacoModule = m;
        this.editor = ed;
    }

    public clearDiagnostics() : boolean {
        let empty: IDiagnostic[] = [];
        return this.setDiagnostics(empty);
    }

    public setDiagnostics(diag: IDiagnostic[]) : boolean {
        let markers = this.convertDiagnosticsToMarkerData(diag, this.editor.getModel());
        let modifiedDiagnostics = false;
        if (!deepEqual(this.currentMarkerData, markers)) {
            this.currentMarkerData = markers;
            // We pass an arbitrary string so monaco can know which diagnostics are
            // owned by our langage service
            this.monacoModule.editor.setModelMarkers(this.editor.getModel(), "trydotnetdiagnostics", markers);
            modifiedDiagnostics = true;
        }
        return modifiedDiagnostics;
    }

    private convertDiagnosticsToMarkerData(diagnostics: IDiagnostic[], model: monacoEditor.editor.ITextModel): monacoEditor.editor.IMarkerData[] {
        let result: monacoEditor.editor.IMarkerData[] = [];
        for (let diagnostic of diagnostics) {
            let start = model.getPositionAt(diagnostic.start);
            let marker: monacoEditor.editor.IMarkerData = {
                severity: diagnostic.severity,
                startLineNumber: start.lineNumber,
                startColumn: start.column,
                endLineNumber: start.lineNumber,
                endColumn: start.column,
                message: diagnostic.message
            };
            result.push(marker);
        }

        return result;
    }
}
