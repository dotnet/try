// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface IRequestIdGenerator {
    getNewRequestId(): Promise<string>;
}

export class RequestIdGenerator implements IRequestIdGenerator {
    private static instanceId = 1;

    private id: string;
    private seed: number;
    constructor() {
        this.id = `trydotnetjs.session.${RequestIdGenerator.instanceId++}`;
        this.seed = 0;

    }
    public getNewRequestId(): Promise<string> {
        const requestId = `${this.id}_${this.seed++}`;


        return Promise.resolve(requestId);
    }
}