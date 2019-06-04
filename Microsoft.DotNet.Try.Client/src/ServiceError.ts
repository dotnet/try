// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Response } from "./MlsClient";

export default class ServiceError extends Error {
    constructor(public readonly statusCode: number, public readonly message: string = "", public readonly requestId: string = "") {
        super(`ServiceError: ${statusCode}: ${message}`);
    }

    public static From(response: Response): ServiceError {
        return new ServiceError(
            response.status,
            response.statusText,
            response.headers && response.headers.map
                ? response.headers.map["request-id"]
                : "");
    }
}
