// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export function wait(ms: number): Promise<void> {
    return new Promise<void>((resolve, _reject) => {
        setTimeout(resolve, ms);
    });
}
