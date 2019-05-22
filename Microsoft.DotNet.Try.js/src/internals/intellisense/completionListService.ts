// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IMessageBus } from "../messageBus";
import { Workspace } from "../workspace";
import { Region } from "../../editableDocument";
import { IRequestIdGenerator } from "../requestIdGenerator";
import { CompletionResult } from "../../completion";
import { Observer } from "rxjs";
import { ServiceError } from "../..";

export class completionListService {
    constructor(private messageBus: IMessageBus, private requestIdGenerator: IRequestIdGenerator, private serviceErrorChannel : Observer<ServiceError>) {
        if (!this.messageBus) {
            throw new Error("messageBus cannot be null");
        }

        if (!this.requestIdGenerator) {
            throw new Error("requestIdGenerator cannot be null");
        }
    }

    async getCompletionList(workspace: Workspace, fileName: string, position: number, region?: Region): Promise<CompletionResult> {
        if (!workspace) {
            throw new Error("workspace cannot be null");
        }

        const requestId = await this.requestIdGenerator.getNewRequestId();

        throw new Error("Method not implemented.");
    }
}