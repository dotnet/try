// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DocumentId } from "./internals/document";

export type Region = string;

export interface IDocument {
    id(): DocumentId;
    setContent(content: string): Promise<void>;
    getContent(): string;
}
