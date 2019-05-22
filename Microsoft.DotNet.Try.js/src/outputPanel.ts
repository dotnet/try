// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ITextDisplay } from "./textDisplay";

export interface IOutputPanel extends ITextDisplay {
    initialise(): void;
    clear(): Promise<void>;
    write(content: string): Promise<void>;
    write(content: string[]): Promise<void>;
    append(content: string): Promise<void>;
    append(content: string[]): Promise<void>;
}



