// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import { ReactNode } from "react";
import { Provider } from "react-redux";
import { Route, RouteProps } from "react-router-dom";
import { AnyAction, Middleware } from "redux";
import actions from "../actionCreators/actions";
import { configureWorkspace, applyScaffolding, getProjectTemplateFromStore } from "../actionCreators/workspaceActionCreators";
import getStore from "../app.store";
import actionToHostMessage from "../mappers/actionToHostMessage";
import map from "../mappers/hostMessageToAction";
import getMiddlewareForLogger, { ActionLogger } from "../middleware/LoggingMiddleware";
import MlsClient from "../MlsClient";
import fetch from "../utilities/fetch";
import { IStore } from "../IStore";
import { CookieGetter } from "../constants/CookieGetter";
import { CookieSetter } from "../constants/CookieSetter";
import { AIClientFactory, IApplicationInsightsClient } from "../ApplicationInsights";
import * as types from "../constants/ActionTypes";

const embed = (props: BrowserAdapterProps) => {
    return (match: RouteProps) => {

        function extractSrcUri(match: RouteProps) {
            var uri = undefined;

            if (match.location && match.location.pathname) {
                uri = match.location.pathname;
                if (match.location.search) {
                    uri += match.location.search;
                }
            }
            return uri;
        }

        let query = new URLSearchParams(match.location.search);
        let hostOrigin = props.iframeWindow.getHostOrigin();
        let referrer = props.iframeWindow.getReferrer();
        let apiBaseAddress = props.iframeWindow.getApiBaseAddress();
        let editorId = query.get("editorId") || "";
        let srcUri = extractSrcUri(match);
        let store = configureStore(props,
            query,
            srcUri,
            hostOrigin,
            referrer,
            apiBaseAddress,
            editorId);

        configurePreviewFeatures(store, query);
        configureInstrumentation(store, query);
        configureHostListener(store, props.iframeWindow);




        if (store.getState().config.useLocalCodeRunner) {
            store.dispatch(actions.configureEditorId(editorId));
            store.dispatch(actions.hostEditorReady(editorId));
        }
        else {
            store.dispatch(actions.hostListenerReady(editorId));
            store.dispatch(actions.hostEditorReady(editorId));
            store.dispatch(actions.hostRunReady(editorId));
        }

        store.dispatch(actions.loadSource());

        return (
            <Provider store={store}>
                {props.children}
            </Provider>
        );
    };
};

const configureStore = function (
    props: BrowserAdapterProps, query: URLSearchParams, srcUri: string, hostOrigin: URL, referrer: URL, apiBaseAddress: URL, editorId: string) {

    let store = getStore(getMiddleware(props.log,
        props.hostWindow,
        query,
        srcUri,
        hostOrigin,
        editorId));

    let workspaceParameter = query.get("workspace");
    let workspaceTypeParameter = query.get("workspaceType");
    let useWasmRunner = (!!(query.get("useWasmRunner"))) === true;

    // Access query string of parent window
    let clientParams = props.iframeWindow.getClientParameters();
    if (clientParams && clientParams.workspaceType) {
        workspaceTypeParameter = clientParams.workspaceType;
    }

    if (clientParams && clientParams.hasOwnProperty("useWasmRunner") && clientParams.useWasmRunner !== null && clientParams.useWasmRunner !== undefined) {
        // useWasmRunner can be set via clientParameters
        useWasmRunner = useWasmRunner || ((!!(clientParams.useWasmRunner)) === true);
    }

    // Get attributes from the srcript element that contains bundeljs
    let fromParameter = query.get("from");
    let bufferIdParameter = query.get("bufferId");
    let fromGistParameter = query.get("fromGist");
    let canShowGitHubPanelQueryParameter = query.get("canShowGitHubPanel");

    configureWorkspace({
        store,
        workspaceParameter,
        workspaceTypeParameter,
        language: "csharp",
        fromParameter,
        bufferIdParameter,
        fromGistParameter,
        canShowGitHubPanelQueryParameter
    });

    if (query.get("completion")) {
        let completionProvider = decodeURIComponent(query.get("completion"));
        store.dispatch(actions.setCompletionProvider(completionProvider));
    }
    else {
        if (Math.random() < props.pythiaPercent) {
            store.dispatch(actions.setCompletionProvider("pythia"));
        }
    }

    if (useWasmRunner) {
        store.dispatch(actions.configureWasmRunner());
    }

    if ((!!(query.get("waitForConfiguration"))) !== false) {
        store.dispatch(actions.hideEditor());
        store.dispatch(actions.disableBranding());
    }

    store.dispatch(actions.enableClientTelemetry(props.aiFactory(referrer)));

    if (clientParams && clientParams.scaffold) {
        store.dispatch(applyScaffolding(clientParams.scaffold,
            "file.cs",
            ["System", "System.Collections.Generic", "System.Linq"]));
    }

    let aiClient = store.getState().config.applicationInsightsClient;
    let client = new MlsClient(fetch,
        hostOrigin,
        props.getCookie,
        aiClient,
        apiBaseAddress);

    store.dispatch(actions.setClient(client));

    store.dispatch(actions.configureBranding());

    setupTelemetryMiddleware(aiClient, store);

    return store;
};

function setupTelemetryMiddleware(aiClient: IApplicationInsightsClient, store: IStore) {
    actionToTelemetry = (action: AnyAction) => {
        if (aiClient) {
            switch (action.type) {
                case types.RUN_CODE_SUCCESS:
                    let projectTemplate = getProjectTemplateFromStore(store);
                    let eventName = `ClientRun.${action.succeeded ? "Success" : "Failure"}`;
                    aiClient.trackEvent(
                        eventName,
                        {
                            requestId: action.requestId,
                            projectTemplate: projectTemplate,
                            executionStrategy: action.executionStrategy
                        });
                    break;
            }
        }
    };
}

const configurePreviewFeatures = function (store: IStore, query: URLSearchParams) {
    let preview = query.get("preview");
    if (preview && preview.toLowerCase() === "true") {
        store.dispatch(actions.enablePreviewFeatures());
    }
};

const configureInstrumentation = (store: IStore, query: URLSearchParams) => {
    const instrumentFlag = query.get("instrument");
    if (instrumentFlag && instrumentFlag.toLowerCase() === "true") {
        store.dispatch(actions.enableInstrumentation());
    }
};

let actionToTelemetry: (toLog: AnyAction) => void = undefined;

const getMiddleware = function (log: ActionLogger,
    hostWindow: ICanPostMessage,
    query: URLSearchParams,
    srcUri: string,
    hostOrigin: URL,
    editorId: string): Middleware[] {
    let middleware: Middleware[] = [];

    if (typeof log === "function") {
        let debug = query.get("debug");
        if (debug && debug.toLowerCase() === "true") {
            middleware.push(getMiddlewareForLogger(toLog => log(toLog)));
        }
    }

    middleware.push(
        getMiddlewareForLogger((toLog: AnyAction) => {
            let result = actionToHostMessage(toLog, srcUri, editorId);

            if (result) {
                hostWindow.postMessage(result, hostOrigin.href);
            }

            if (actionToTelemetry) {
                actionToTelemetry(toLog);
            }
        })
    );

    return middleware;
};

const configureHostListener = function (
    store: IStore,
    iframeWindow: ICanAddEventListener<MessageEvent> & IIFrameWindow) {

    let hostOrigin = iframeWindow.getHostOrigin();

    store.dispatch(actions.notifyHostProvidedConfiguration({ hostOrigin }));
    iframeWindow.addEventListener("message", (e: MessageEvent) => {
        let mappedEvent = map(e.data);

        if (mappedEvent) {
            store.dispatch(mappedEvent);
        }
    });
};

export interface BrowserAdapterProps {
    children?: ReactNode;
    getCookie?: CookieGetter;
    setCookie?: CookieSetter;
    log?: ActionLogger;
    hostWindow: IHostWindow;
    pythiaPercent?: number;
    iframeWindow: IIFrameWindow;
    aiFactory?: AIClientFactory;
}

export interface IHostWindow extends ICanPostMessage {
}

export interface IIFrameWindow extends IHaveQueryParams, ICanAddEventListener<MessageEvent> {
    getHostOrigin(): URL;
    getReferrer(): URL;
    getApiBaseAddress(): URL;
    getClientParameters(): ClientParameters;
}

export interface IHaveQueryParams {
    getQuery(): URLSearchParams;
}

const BrowserAdapter: React.SFC<BrowserAdapterProps> = (props) => (
    <Route component={embed(props)} />
);

BrowserAdapter.defaultProps = {
    children: <div />,
    log: () => { },
    hostWindow: null,
    pythiaPercent: 0,
    iframeWindow: null
};

export interface ClientParameters {
    workspaceType?: string;
    scaffold?: string;
    referrer?: URL;
    useWasmRunner?: boolean;
}

export type ICanPostMessage = { postMessage: (message: Object, targetOrigin: string) => void };

export type ICanAddEventListener<T> = { addEventListener: (type: string, listener: (message: T) => void) => void };

export default BrowserAdapter;
