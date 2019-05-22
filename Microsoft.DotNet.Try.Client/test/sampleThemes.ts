// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as monacoEditor from "monaco-editor";

const sampleThemes: { [x: string]: monacoEditor.editor.IStandaloneThemeData } = {
  myCustomTheme: {
    base: "vs", // can also be vs-dark or hc-black
    colors: {},
    inherit: true, // can also be false to completely replace the builtin rules
    rules: [
      { token: "comment", foreground: "ffa500", fontStyle: "italic underline" },
      { token: "comment.js", foreground: "008800", fontStyle: "bold" },
      { token: "comment.css", foreground: "0000ff" } // will inherit fontStyle from `comment` above
    ]
  }
};

export default sampleThemes;
