// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IInstrumentation, IFilePosition, IVariableLocation, IVariable, IFileLocationSpan } from "../IState";
import * as classnames from "classnames";
import * as styles from "../dot.net.style.app.css";
import createCodeLens, { convertToOneBasedIndex } from "../utilities/monacoUtilities";
import { groupBy, flatten } from "../utilities/arrayUtilities";
import { IEditorState, IEditorProps } from "./Editor";
import * as monacoEditor from "monaco-editor";

export default class InstrumentationAdapter {
    private editor: monacoEditor.editor.ICodeEditor;

    private oldDecorations: string[];

    private currentCodeLens: monacoEditor.IDisposable;

    constructor(ed: monacoEditor.editor.ICodeEditor) {
        this.editor = ed;
        this.oldDecorations = [];
    }

    public setOrUpdateInstrumentation(oldProps: IEditorProps, props: IEditorProps, state: IEditorState) {

        const canShowInstrumentation = state
            && props.runState
            && props.runState.instrumentation;

        if (canShowInstrumentation) {
            const instrumentation = props.runState.instrumentation[props.runState.currentInstrumentationStep];

            const variables = [].concat(instrumentation.locals)
                .concat(instrumentation.parameters)
                .concat(instrumentation.fields)
                .filter(x => x);

            const instrumentationUpdated =
                !this.currentCodeLens ||
                oldProps.runState.currentInstrumentationStep !== props.runState.currentInstrumentationStep;

            const canShowCodeLens = props.runState.variableLocations && variables && instrumentationUpdated;

            if (canShowCodeLens) {
                if (this.currentCodeLens) {
                    this.currentCodeLens.dispose();
                }
                this.currentCodeLens = this.registerCodeLens(
                    variables,
                    props.runState.variableLocations,
                    state.monacoModule
                );
            }
            this.updateHighlightedLine(instrumentation);
        }
    }

    public updateHighlightedLine(instrumentation: IInstrumentation) {
        this.oldDecorations = this.editor.deltaDecorations(this.oldDecorations, this.GetDecorations(instrumentation));
        if (instrumentation.filePosition) {
            this.editor.revealLineInCenterIfOutsideViewport(instrumentation.filePosition.line);
        }
    }

    public clearInstrumentation() {
        this.oldDecorations = this.editor.deltaDecorations(this.oldDecorations, []);
        if (this.currentCodeLens) {
            this.currentCodeLens.dispose();
        }
    }

    public registerCodeLens(variables: IVariable[], variableLocations: IVariableLocation[], monacoModule: typeof monacoEditor): monacoEditor.IDisposable {
        const codeLensSymbols = this.setupCodeLenses(variables, variableLocations);
        return monacoModule.languages.registerCodeLensProvider("csharp", {
            provideCodeLenses: () => {
                return codeLensSymbols;
            },
            resolveCodeLens: (_unused: any, codeLens: monacoEditor.languages.ICodeLensSymbol) => codeLens
        });
    }

    private setupCodeLenses(variables: IVariable[], variableLocations: IVariableLocation[]): monacoEditor.languages.ICodeLensSymbol[] {
        const codeLensSymbols = variables.map(variable => {
            const variableWithLocations = variableLocations.find(v => v.declaredAt.start === variable.declaredAt.start);
            if (variableWithLocations === null || variableWithLocations === undefined) {
                return;
            }

            const displayString = variable.name + " = " + variable.value;
            const groupByLine = groupBy<IFileLocationSpan, number>(location => location.startLine);
            const variableLocationsByLine = groupByLine(variableWithLocations.locations)
            const earliestVariableLocationByLine = this.takeLocationWithSmallestColumn(variableLocationsByLine);
            return earliestVariableLocationByLine.map(location => createCodeLens(displayString, location.startColumn, location.startLine));
        });

        return flatten(codeLensSymbols);
    }

    private takeLocationWithSmallestColumn(locations: Map<number, IFileLocationSpan[]>): IFileLocationSpan[] {
        const variableLocations: IFileLocationSpan[] = [];
        locations.forEach((v, _k, _map) => {
            const locationWithSmallestColumn = v.reduce((acc, next) => (next.startColumn < acc.startColumn) ? next : acc);
            variableLocations.push(locationWithSmallestColumn);
        });
        return variableLocations;
    }

    private GetDecorations(instrumentation: IInstrumentation): monacoEditor.editor.IModelDeltaDecoration[] {
        let decorations: monacoEditor.editor.IModelDeltaDecoration[] = [];

        if (instrumentation.filePosition) {
            decorations.push(this.GetLineDecoration(instrumentation.filePosition));
        }

        return decorations;
    }

    private GetLineDecoration(position: IFilePosition): monacoEditor.editor.IModelDeltaDecoration {
        return {
            range: {
                startLineNumber: convertToOneBasedIndex(position.line),
                startColumn: 0,
                endLineNumber: convertToOneBasedIndex(position.line),
                endColumn: 0
            },
            options: {
                isWholeLine: true,
                className: classnames(styles.highlightedLine)
            }
        };
    }

}
