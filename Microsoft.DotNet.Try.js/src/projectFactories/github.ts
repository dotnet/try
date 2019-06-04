// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Project } from "../project";
import { ISession } from "../session";
import { Session } from "../internals/session";
import { responseFor } from "../internals/responseFor";
import { CREATE_PROJECT_RESPONSE, CREATE_PROJECT_FROM_GIST_REQUEST, ApiMessage } from "../internals/apiMessages";

export async function createProjectFromGist(session: ISession, packageName: string, gistId: string, hash?: string): Promise<Project> {
    let internalSession = <Session>session;
    let requestId = await internalSession.getRequestIdGenerator().getNewRequestId();
    let ret = responseFor<Project>(internalSession.getMessageBus(), CREATE_PROJECT_RESPONSE, requestId, (response: any) => {

        if (response.success) {
            return response.project;
        } else {
            throw new Error(response.error);
        }
    });

    let request: ApiMessage = {
        type: CREATE_PROJECT_FROM_GIST_REQUEST,
        gistId: gistId,
        projectTemplate: packageName,
        commitHash: hash,
        requestId: requestId
    }

    internalSession.getMessageBus().post(request);
    return ret;
}