// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export interface ClientConfiguration {
  versionId: string;
  defaultTimeoutMs: number;
  enableBranding: boolean;
  applicationInsightsKey: string;
  _links: {
    _self: RequestDescriptor;
    [propName: string]: RequestDescriptor;
  };
}

export interface RequestDescriptor {
  href: string;
  method: string;
  templated?: boolean;
  properties?: Array<{ name: string; value?: any }>;
  body?: { [propName: string]: any };
  timeoutMs?: number;
}

export interface ApiRequest {
  url: URL;
  method: string;
  timeoutMsHeader: string;
}

export function extractApiRequest(
  requestDescriptor: RequestDescriptor,
  apiParameters: ApiParameters,
  hostOrigin: URL,
  apiBaseAddress: URL
): ApiRequest {
    let url = new URL(apiBaseAddress.href);

    url.pathname = requestDescriptor.href;

    if (requestDescriptor.templated) {
      url.pathname = composeUrl(requestDescriptor, apiParameters);
    }

    composeQueryString(
      url.searchParams,
      requestDescriptor, 
      apiParameters, 
      hostOrigin);

    return {
      url,
      method: requestDescriptor.method,
      timeoutMsHeader: `${requestDescriptor.timeoutMs}`
    };
}

function clearUnsetOptionalSymbols(url: string): string {
  let filter = /(\/\{[^{}?]+\?\})/gi;
  return url.replace(filter, "");
}

function replaceSymbol(url: string, symbol: string, value: any): string {
  if (value === undefined) {
    return url;
  }
  let filter = new RegExp(`(\\{${symbol}\\??\\})`, "gi");
  return url.replace(filter, `${value}`);
}

function composeUrl(apiLink: RequestDescriptor, apiParameters?: { [propName: string]: any }) {
  let url = apiLink.href;
  if (apiParameters) {
    for (let prop in apiParameters) {
      url = replaceSymbol(url, prop, apiParameters[prop]);
    }
  }
  url = clearUnsetOptionalSymbols(url);
  return url;
}

function composeQueryString(
  queryString : URLSearchParams,
  apiLink: RequestDescriptor,
  apiParameters: ApiParameters,
  hostOrigin: URL) : void {
    
  if (hostOrigin) {
    queryString.append("hostOrigin", hostOrigin.href);
  }

  if (apiLink.properties && apiParameters) {
    apiLink.properties.forEach(property => {

      let parameter = apiParameters[property.name];
      
      if (parameter !== undefined) {
        
        if (isURL(parameter)) {
          parameter = parameter.href;
        }

        queryString.append(property.name, parameter.toString());
      }
    });
  }
}

export type ApiParameter = boolean | string | number | URL;

export type ApiParameters = { [propName: string]: ApiParameter };

function isURL(value: ApiParameter) : value is URL
{
  return (value !== null && value !== undefined )&& ((<URL>value).href !== undefined);
}
