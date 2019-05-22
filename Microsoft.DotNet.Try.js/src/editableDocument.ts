// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export type Region = string;

export interface IDocument {
    id(): string;
    setContent(content: string): Promise<void>;
    getContent(): string;
}
