// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Diagnostic } from "./diagnostics";

export type SignatureHelpResult = {
    signatures: SignatureHelpItem[],
    diagnostics: Diagnostic[]
}

export type SignatureHelpItem = {
    name: string,
    label: string,
    documentation: string,
    parameters: SignatureHelpParameter[],
}

export type SignatureHelpParameter = {
    name: string,
    label: string,
    documentation: string,
}