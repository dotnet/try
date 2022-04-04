// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from ".";

export type CompletionResult = {
    items: CompletionItem[],
    diagnostics?: Diagnostic[]
}

export type CompletionItem = {
    DisplayText: string,
    Kind: string,
    FilterText: string,
    SortText: string,
    InsertText: string,
    Documentation: string,
    AcceptanceUri?: string
}