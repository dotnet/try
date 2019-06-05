// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MonacoEditorConfiguration } from "./editor";

export type Configuration = {
    hostOrigin?: string,
    trydotnetOrigin?: string,
    debug?: boolean,
    useWasmRunner?: boolean,
    enablePreviewFeatures?: boolean,
    enableGithubPanel?: boolean,
    editorConfiguration?: MonacoEditorConfiguration,
    runAsCodeChanges?: boolean
}
