// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

let id = "trydotnet.client";
let seed = 0;

export function newOperationId() : string{
    let operationId: string;
    try{
        operationId = Microsoft.ApplicationInsights.UtilHelpers.newId();
    }
    catch{
        operationId =  `${id}_${seed++}`;
    }
    return  operationId;
}
