// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// workaround: https://github.com/Microsoft/ApplicationInsights-JS/issues/476#issuecomment-341041379
// @ts-ignore 
global.define = () => {};

import { AppInsights } from "applicationinsights-js";

export interface IApplicationInsightsClient {
    trackEvent(name: string, properties?: { [key: string]: string }, measurements?: { [key: string]: number }): void;
    trackDependency(method: string, absoluteUrl: string, pathName: string, totalTime: number, success: boolean, resultCode: number, operationId?: string): void;
    trackException(exception: Error, handledAt?: string, properties?: { [key: string]: string }, measurements?: { [key: string]: number }, severityLevel?: AI.SeverityLevel): void;
}

export interface AIClientFactory {
    (referrer: URL): IApplicationInsightsClient;
}

export class NullAIClient implements IApplicationInsightsClient {
    public trackException(_exception: Error, _handledAt?: string, _properties?: { [key: string]: string; }, _measurements?: { [key: string]: number; }, _severityLevel?: AI.SeverityLevel): void {
    }

    public trackEvent(_name: string, _properties?: { [key: string]: string; }, _measurements?: { [key: string]: number; }): void {
    }

    public trackDependency(_method: string, _absoluteUrl: string, _pathName: string, _totalTime: number, _success: boolean, _resultCode: number, _operationId?: string): void {
    }
}

function addReferrerToProperties(referrer: URL, properties?: { [key: string]: string; }): { [key: string]: string; } {
    if (referrer !== undefined && referrer !== null) {
        if (!properties) {
            properties = {};
        }
        properties["referrer"] = referrer.href;
    }
    return properties;
}

export class ApplicationInsightsClient implements IApplicationInsightsClient {
    constructor(private referrer: URL) {
    }

    public trackException(exception: Error, handledAt?: string, properties?: { [key: string]: string; }, measurements?: { [key: string]: number; }, severityLevel?: AI.SeverityLevel): void {
        if (AppInsights && AppInsights.trackException) {
            try {
                properties = addReferrerToProperties(this.referrer, properties);
                AppInsights.trackException(
                    exception,
                    handledAt,
                    properties,
                    measurements,
                    severityLevel);
            }
            catch (ex) {
            }
        }
    }

    public trackEvent(name: string, properties?: { [key: string]: string; }, measurements?: { [key: string]: number; }): void {
        if (AppInsights && AppInsights.trackDependency) {
            try {
                properties = addReferrerToProperties(this.referrer, properties);
                AppInsights.trackEvent(
                    name,
                    properties,
                    measurements);

            }
            catch (ex) {
            }
        }
    }

    public trackDependency(method: string, absoluteUrl: string, pathName: string, totalTime: number, success: boolean, resultCode: number, operationId?: string): void {
        if (AppInsights && AppInsights.trackDependency) {
            try {
                AppInsights.trackDependency(operationId ? operationId : this.GetAiId(), method, absoluteUrl, pathName, totalTime, success, resultCode);
            }
            catch (ex) {
            }
        }
    }

    private GetAiId(): string {
        try {
            return Microsoft.ApplicationInsights.UtilHelpers.newId();
        }
        catch (ex) {
            return "an-id";
        }
    }
}
