// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";
import * as monacoEditor from "monaco-editor";
import * as styles from "../../../src/dot.net.style.app.css";
import * as classnames from "classnames";

import { IInstrumentation, IVariable, IVariableLocation } from "../../../src/IState";

import InstrumentationAdapter from "../../../src/components/InstrumentationAdapter";
import createCodeLens from "../../../src/utilities/monacoUtilities";
import { suite } from "mocha-typescript";


suite("InstrumentationAdapter", () => {

    let decorationsSet: any = [];
    const stubEditor = {
        deltaDecorations(_unused: any, decorations: any) { decorationsSet = decorations; },
        addOverlayWidget(widget: any) { decorationsSet = widget; }
    };

    beforeEach(() => {
        decorationsSet = [];
    });

    it("sets line indicator decorations on editor", () => {
        const instrumentation = {
            filePosition: { line: 0 }
        };

        const expectedDecorations = [{
            range: {
                startLineNumber: 1,
                startColumn: 0,
                endLineNumber: 1,
                endColumn: 0
            },
            options: {
                isWholeLine: true,
                className: classnames(styles.highlightedLine)
            }
        }];

        const stubEditor = {
            deltaDecorations(_unused: any, decorations: any) {  decorationsSet = decorations; },
            revealLineInCenterIfOutsideViewport(_unused: number) { }
        };

        const adapter = new InstrumentationAdapter(stubEditor as monacoEditor.editor.ICodeEditor);
        adapter.updateHighlightedLine(instrumentation as IInstrumentation);

        decorationsSet.should.deep.equal(expectedDecorations);
    });

    it("creates code lens symbols from variables", () => {
        const instrumentation = {
            locals: [{
                name: "a",
                value: "1",
                declaredAt: {
                    start: 1,
                    end: 2
                }
            },
            {
                name: "b",
                value: "2",
                declaredAt: {
                    start: 3,
                    end: 4
                }
            }]
        };

        const locations = [{
            name: "a",
            locations: [
                {
                    startLine: 1,
                    endLine: 1,
                    startColumn: 1,
                    endColumn: 2
                },
                {
                    startLine: 0,
                    endLine: 0,
                    startColumn: 1,
                    endColumn: 2
                }
            ],
            declaredAt: { start: 1, end: 2 }
        }, {
            name: "b",
            locations: [
                {
                    startLine: 3,
                    endLine: 3,
                    startColumn: 1,
                    endColumn: 2
                }
            ],
            declaredAt: { start: 3, end: 4 }
        }];

        const expectedDecorations = [
            createCodeLens("a = 1", 1, 0),
            createCodeLens("a = 1", 1, 1),
            createCodeLens("b = 2", 1, 3)
        ];

        const adapter = new InstrumentationAdapter(stubEditor as monacoEditor.editor.ICodeEditor);

        var codeLensProvider: any = [];
        const monacoModule = {
            languages: {
                registerCodeLensProvider: (_unused: string, provider: monacoEditor.languages.CodeLensProvider) => {
                    codeLensProvider = provider;
                }
            }
        };

        adapter.registerCodeLens(
            instrumentation.locals as IVariable[],
            locations as IVariableLocation[],
            monacoModule as typeof monacoEditor);
            
        codeLensProvider.provideCodeLenses(null, null).should.have.deep.members(expectedDecorations);
    });

    it("only displays one codelens per variable per line", () => {
        const instrumentation = {
            locals: [{
                name: "a",
                value: "1",
                declaredAt: {
                    start: 1,
                    end: 2
                }
            },
            {
                name: "b",
                value: "2",
                declaredAt: {
                    start: 3,
                    end: 4
                }
            }]
        };

        const locations = [{
            name: "a",
            locations: [
                {
                    startLine: 1,
                    endLine: 1,
                    startColumn: 1,
                    endColumn: 2
                },
                {
                    startLine: 0,
                    endLine: 0,
                    startColumn: 1,
                    endColumn: 2
                }
            ],
            declaredAt: { start: 1, end: 2 }
        }, {
            name: "b",
            locations: [
                {
                    startLine: 3,
                    endLine: 3,
                    startColumn: 1,
                    endColumn: 2
                },
                {
                    startLine: 3,
                    endLine: 3,
                    startColumn: 3,
                    endColumn: 4
                }
            ],
            declaredAt: { start: 3, end: 4 }
        }];

        const expectedDecorations = [
            createCodeLens("a = 1", 1, 0),
            createCodeLens("a = 1", 1, 1),
            createCodeLens("b = 2", 1, 3)
        ];

        const adapter = new InstrumentationAdapter(stubEditor as monacoEditor.editor.ICodeEditor);

        var codeLensProvider: any = [];
        const monacoModule = {
            languages: {
                registerCodeLensProvider: (_unused: string, provider: monacoEditor.languages.CodeLensProvider) => {
                    codeLensProvider = provider;
                }
            }
        };

        adapter.registerCodeLens(
            instrumentation.locals as IVariable[],
            locations as IVariableLocation[],
            monacoModule as typeof monacoEditor);
            
        codeLensProvider.provideCodeLenses(null, null).should.have.deep.members(expectedDecorations);
    });
});
