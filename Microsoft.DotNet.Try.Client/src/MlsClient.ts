// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICanFetch } from "./MlsClient";
import IMlsClient, { IMlsCompletionItem, IRunRequest, IWorkspaceRequest, IWorkspaceResponse, IGistWorkspace, IRunResponse, ApiError, ICompileResponse, CompletionResult, SignatureHelpResult, DiagnosticResult } from "./IMlsClient";
import { CookieGetter } from "./constants/CookieGetter";
import { IWorkspace } from "./IState";
import ServiceError from "./ServiceError";
import { ClientConfiguration, RequestDescriptor, extractApiRequest, ApiRequest } from "./clientConfiguration";
import ICompletionItem from "./ICompletionItem";
import { IApplicationInsightsClient } from "./ApplicationInsights";
import { CreateProjectFromGistRequest, CreateProjectResponse, CreateRegionsFromFilesRequest, CreateRegionsFromFilesResponse } from "./clientApiProtocol";

export interface ICanFetch {
  (uri: string, request: Request): Promise<Response>;
}

export interface Headers {
  [key: string]: string;
}

export interface Request {
  body?: string;
  credentials?: RequestCredentials;
  headers: Headers;
  method: string;
}

export interface Response {
  json?: () => Promise<any>;
  text?: () => Promise<string>;
  ok: boolean;
  status?: number;
  statusText?: string;
  headers?: {
    map: {
      [x: string]: string;
    };
  };
}

const XsrfTokenKey = "XSRF-TOKEN";

const acceptCompletionKey = "acceptCompletion";
const signatureHelpKey = "signatureHelp";
const completionKey = "completion";
const diagnosticsKey = "diagnostics";
const loadFromGistKey = "loadFromGist";
const runKey = "run";
const compileKey = "compile";
const snippetKey = "snippet";
const projectFromGistKey = "projectFromGist";
const regionsFromFilesKey = "regionsFromFiles";

export default class MlsClient implements IMlsClient {
  constructor(fetch: ICanFetch, 
              hostOrigin: URL, 
              getCookie: CookieGetter = null, 
              aiClient: IApplicationInsightsClient, 
              trydotnetBaseAddress: URL) {
    this.fetch = fetch;
    this.trydotnetBaseAddress = trydotnetBaseAddress;
    this.getCookie = getCookie;
    this.clientConfigurationUri = new URL(this.trydotnetBaseAddress.href);
    this.clientConfigurationUri.pathname = "clientConfiguration";
    this.hostOrigin = hostOrigin;
    this.clientConfiguration = null;
    this.aiClient = aiClient;

    if (hostOrigin) {
      this.clientConfigurationUri.searchParams.append("hostOrigin", hostOrigin.href);
    }
  }

  private aiClient: IApplicationInsightsClient;
  private fetch: ICanFetch;
  private trydotnetBaseAddress: URL;
  private clientConfigurationUri: URL;
  private getCookie: CookieGetter;
  private clientConfiguration: ClientConfiguration;

  private runRequestDescriptor: RequestDescriptor;

  private compileRequestDescriptor: RequestDescriptor;
  private snippetRequestDescriptor: RequestDescriptor;
  private loadFromGistRequestDescriptor: RequestDescriptor;
  private completionRequestDescriptor: RequestDescriptor;
  private signatureHelpRequestDescriptor: RequestDescriptor;
  private acceptCompletionItemRequestDescriptor: RequestDescriptor;
  private diagnosticsRequestDescriptor: RequestDescriptor;

  private projectFromGistRequestDescriptor: RequestDescriptor;
  private regionsFromFilesRequestDescriptor: RequestDescriptor;

  private hostOrigin: URL;

  public async getSourceCode(from: IWorkspaceRequest): Promise<IWorkspaceResponse> {
    return this.fetchWithConfigurationRetry<IWorkspaceResponse>(
      () => this.snippetRequestDescriptor,
      { from: from.sourceUri });
  }

  public compile(runRequest: IRunRequest): Promise<ICompileResponse> {
    return this.fetchWithConfigurationRetry<ICompileResponse>(
      () => this.compileRequestDescriptor,
      null,
      runRequest);
  }

  public run(runRequest: IRunRequest): Promise<IRunResponse> {
    return this.fetchWithConfigurationRetry<IRunResponse>(
      () => this.runRequestDescriptor,
      null,
      runRequest);
  }

  public async getWorkspaceFromGist(gistId: string, workspaceType: string, extractBuffers: boolean = false): Promise<IGistWorkspace> {
    let gistInfo = this.parseGistId(gistId);
    return this.fetchWithConfigurationRetry<IGistWorkspace>(
      () => this.loadFromGistRequestDescriptor,
      { workspaceType, extractBuffers, gistId: gistInfo.gist, commitHash: gistInfo.commitHash });
  }

  public async createProjectFromGist(request: CreateProjectFromGistRequest): Promise<CreateProjectResponse> {
    return this.fetchWithConfigurationRetry<CreateProjectResponse>(
      () => this.projectFromGistRequestDescriptor,
      null,
      request);
  }

  public async  createRegionsFromProjectFiles(request: CreateRegionsFromFilesRequest): Promise<CreateRegionsFromFilesResponse> {
    return this.fetchWithConfigurationRetry<CreateRegionsFromFilesResponse>(
      () => this.regionsFromFilesRequestDescriptor,
      null,
      request);
  }

  public async getCompletionList(workspace: IWorkspace, bufferId: string, position: number, completionProvider: string): Promise<CompletionResult> {
    let result = await this.fetchWithConfigurationRetry<any>(
      () => this.completionRequestDescriptor,
      { completionProvider },
      {
        workspace,
        activeBufferId: bufferId,
        position: position
      });

    return {
      items: this.mapCompletionKindToMonacoKind(result),
      diagnostics: result.diagnostics
    };
  }

  public async getSignatureHelp(workspace: IWorkspace, bufferId: string, position: number): Promise<SignatureHelpResult> {
    let result = await this.fetchWithConfigurationRetry<any>(
      () => this.signatureHelpRequestDescriptor,
      null,
      {
        workspace,
        activeBufferId: bufferId,
        position
      });

    return {
      signatures: result.signatures,
      activeParameter: result.activeParameter,
      activeSignature: result.activeSignature,
      diagnostics: result.diagnostics
    };
  }

  public async getDiagnostics(workspace: IWorkspace, bufferId: string): Promise<DiagnosticResult> {
    let result = await this.fetchWithConfigurationRetry<any>(
      () => this.diagnosticsRequestDescriptor,
      null,
      {
        workspace,
        activeBufferId: bufferId
      });

    return {
      diagnostics: result.diagnostics
    };
  }

  public async acceptCompletionItem(selection: ICompletionItem): Promise<void> {
    if (selection && selection.acceptanceUri) {
      return this.fetchWithConfigurationRetry<void>(
        () => this.acceptCompletionItemRequestDescriptor,
        {
          acceptanceUri: selection.acceptanceUri
        });
    }
  }

  public async getConfiguration(): Promise<ClientConfiguration> {
    var request = this.createPostRequest({});
    this.addXsrfTokenToRequest(request);
    let configuration = await this.fetchAndHandleErrors(this.clientConfigurationUri, request);
    this.clientConfiguration = <ClientConfiguration>configuration;
    this.processConfiguration();
    return configuration;
  }

  private async fetchWithConfigurationRetry<T>(
    getRequestDescriptor: () => RequestDescriptor,
    apiParameters: any = null,
    body: any = null): Promise<T> {
    await this.ensureConfigurationIsLoaded();
    let requestDescriptor = getRequestDescriptor();

    let api = extractApiRequest(requestDescriptor, 
                                apiParameters, 
                                this.hostOrigin,
                                this.trydotnetBaseAddress);

    let request = this.createRequest(requestDescriptor, body);
    request = this.addHeaders(request, api);

    let response = await this.fetch(api.url.href, request);

    let shouldRetryWithNewConfiguration = await this.shouldRefreshConfigurationAndRetry(response);

    if (!shouldRetryWithNewConfiguration) {
      if (response.ok) {
        return response.json();
      }
      else {
        throw new ServiceError(response.status, response.statusText);
      }
    }

    await this.getConfiguration();
    api = extractApiRequest(requestDescriptor, 
                            apiParameters, 
                            this.hostOrigin,
                            this.trydotnetBaseAddress);

    request = this.createRequest(requestDescriptor, body);
    request = this.addHeaders(request, api);

    let requestId: string = undefined;
    if (body && body.requestId && typeof body.requestId === "string") {
      requestId = body.requestId;
    }
    return this.fetchAndHandleErrors(api.url, request, requestId);
  }

  private mapCompletionKindToMonacoKind(completions: any) {
    var mapKind = (theKind: string) => {
      const _kinds = Object.create(null);
      // types
      _kinds["Class"] = 6; //monaco.languages.CompletionItemKind.Class;
      _kinds["Delegate"] = 6; //monaco.languages.CompletionItemKind.Class; // need a better option for this.
      _kinds["Enum"] = 12; //monaco.languages.CompletionItemKind.Enum;
      _kinds["Interface"] = 7; //monaco.languages.CompletionItemKind.Interface;
      _kinds["Struct"] = 6; //monaco.languages.CompletionItemKind.Class; // Monaco doesn't have a Struct kind
      // variables
      _kinds["Local"] = 5; //monaco.languages.CompletionItemKind.Variable;
      _kinds["Parameter"] = 5; //monaco.languages.CompletionItemKind.Variable;
      _kinds["RangeVariable"] = 5; //monaco.languages.CompletionItemKind.Variable;
      // members
      _kinds["Const"] = 4; //monaco.languages.CompletionItemKind.Field; // Monaco doesn't have a Field kind
      _kinds["EnumMember"] = 12; //monaco.languages.CompletionItemKind.Enum; // Monaco doesn't have an EnumMember kind
      //_kinds['Event'] = monaco.languages.CompletionItemKind.Event;
      _kinds["Field"] = 4; //monaco.languages.CompletionItemKind.Field;
      _kinds["Method"] = 1; //monaco.languages.CompletionItemKind.Method;
      _kinds["Property"] = 9; //monaco.languages.CompletionItemKind.Property;
      // other stuff
      _kinds["Label"] = 10; //monaco.languages.CompletionItemKind.Unit; // need a better option for this.
      _kinds["Keyword"] = 13; //monaco.languages.CompletionItemKind.Keyword;
      _kinds["Namespace"] = 8; //monaco.languages.CompletionItemKind.Module;
      return _kinds[theKind] || 9; //monaco.languages.CompletionItemKind.Property;
    };
    let mappedCompletions: ICompletionItem[] = completions.items.map((i: IMlsCompletionItem) => {
      let mappedItem: ICompletionItem = {
        acceptanceUri: i.acceptanceUri,
        documentation: i.documentation,
        filterText: i.filterText,
        insertText: i.insertText,
        kind: mapKind(i.kind),
        label: i.displayText,
        sortText: i.sortText
      };
      return mappedItem;
    });
    return mappedCompletions;
  }
  private parseGistId(gistId: string): { gist: string; commitHash?: string } {
    let parts = gistId.split("/");
    let result: { gist: string; commitHash?: string } = { gist: parts[0] };
    if (parts.length > 1) {
      result.commitHash = parts[1];
    }
    return result;
  }

  private processConfiguration() {
    if (this.clientConfiguration) {
      this.completionRequestDescriptor = this.clientConfiguration._links[completionKey];
      this.signatureHelpRequestDescriptor = this.clientConfiguration._links[signatureHelpKey];
      this.runRequestDescriptor = this.clientConfiguration._links[runKey];
      this.compileRequestDescriptor = this.clientConfiguration._links[compileKey];
      this.snippetRequestDescriptor = this.clientConfiguration._links[snippetKey];
      this.loadFromGistRequestDescriptor = this.clientConfiguration._links[loadFromGistKey];
      this.acceptCompletionItemRequestDescriptor = this.clientConfiguration._links[acceptCompletionKey];
      this.projectFromGistRequestDescriptor = this.clientConfiguration._links[projectFromGistKey];
      this.regionsFromFilesRequestDescriptor = this.clientConfiguration._links[regionsFromFilesKey];
      this.diagnosticsRequestDescriptor = this.clientConfiguration._links[diagnosticsKey];
    }
  }

  private addXsrfTokenToRequest(request: any) {
    if (this.getCookie !== null) {
      var xsrfToken = this.getCookie(XsrfTokenKey);

      if (xsrfToken) {
        request.headers[XsrfTokenKey] = xsrfToken;
      }
    }
  }

  private createRequest(api: RequestDescriptor, data?: any) {
    var request: Request = {
      method: api.method,
      headers: {
        "Content-Type": "application/json"
      },
      credentials: "same-origin"
    };

    if (api.method === "POST" && data) {
      request.body = JSON.stringify(data);
    }
    this.addXsrfTokenToRequest(request);
    return request;
  }

  private createPostRequest(data: any) {
    var request: Request = {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(data),
      credentials: "same-origin"
    };

    this.addXsrfTokenToRequest(request);

    return request;
  }

  private addHeaders(request: Request, api?: ApiRequest): Request {
    if (this.clientConfiguration) {
      request.headers["ClientConfigurationVersionId"] = this.clientConfiguration.versionId;
    } else {
      request.headers["ClientConfigurationVersionId"] = "undefined";
    }

    request.headers["Timeout"] = this.getApiCallTimeout(api);
    return request;
  }

  private getApiCallTimeout(api: ApiRequest): string {
    let timeOutMs = "15000";
    if (api) {
      timeOutMs = `${api.timeoutMsHeader}`;
    } else if (this.clientConfiguration) {
      timeOutMs = `${this.clientConfiguration.defaultTimeoutMs}`;
    }
    return timeOutMs;
  }

  private async shouldRefreshConfigurationAndRetry(response: Response): Promise<boolean> {
    let needNewConfig = false;
    switch (response.status) {
      case 400:
        let reason: ApiError = await response.json();
        if (reason.code === "ConfigurationVersionError") {
          needNewConfig = true;
        }
        break;
    }
    return needNewConfig;
  }

  private async ensureConfigurationIsLoaded(): Promise<void> {
    if (this.clientConfiguration === null) {
      await this.getConfiguration();
    }
  }

  private async fetchAndHandleErrors(uri: URL, request: Request, reqeustId?: string): Promise<any> {
    try {
      var response = await this.fetch(uri.href, request);

      if (!response) {
        throw new Error("ECONNREFUSED");
      }

      if (this.aiClient !== null) {
        this.aiClient.trackDependency(
          request.method,
          "",
          uri.href,
          1,
          response.ok,
          response.status,
          reqeustId);
      }

      if (response.ok) {
        return response.json();
      }
      
      throw new ServiceError(response.status, response.statusText);
    
    } catch (ex) {
      throw ex;
    }
  }
}
