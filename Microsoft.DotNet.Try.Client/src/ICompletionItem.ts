// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monacoEditor from "monaco-editor";

export default interface ICompletionItem extends monacoEditor.languages.CompletionItem {
    acceptanceUri?: string; 
}
