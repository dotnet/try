// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monacoEditor from "monaco-editor";

export default function createCodeLens(text: string, column: number, line: number, commandId: string | null = null): monacoEditor.languages.ICodeLensSymbol {
    return {
        range: {
            startLineNumber: convertToOneBasedIndex(line),
            startColumn: column,
            endLineNumber: convertToOneBasedIndex(line) + 1,
            endColumn: column
        },
        command: {
            title: text,
            id: commandId
        }
    };
}

export const convertToOneBasedIndex = (line: number) => line + 1;
