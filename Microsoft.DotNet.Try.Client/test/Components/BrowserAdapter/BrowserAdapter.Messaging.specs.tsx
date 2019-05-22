// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import BrowserAdapter, { ICanAddEventListener, IIFrameWindow, IHostWindow } from "../../../src/components/BrowserAdapter";
import { Action } from "redux";
import { MemoryRouter } from "react-router-dom";
import MlsClient from "../../../src/MlsClient";
import actions from "../../../src/actionCreators/actions";
import { expect, should } from "chai";
import fetch from "../../../src/utilities/fetch";
import { fibonacciCode, emptyWorkspace } from "../../testResources";
import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { NullAIClient } from "../../../src/ApplicationInsights";
enzyme.configure({ adapter: new Adapter() });

require("jsdom-global")();

interface ICanSendEvent {
    sendMessageEvent: (origin: string, data: any) => void;
    recordedEvents: Action[];
}

interface ICanRecordPostedMessages {
    recordedMessages: Object[];
}

should();

describe("BrowserAdapter Messaging", () => {
    var recordedActions: Action[];
    var recordAction: (a: Action) => void;
    var hostWindow: IHostWindow & ICanRecordPostedMessages;
    let cookieValue: string;
    const setCookie = (_name: string, value: string) => { cookieValue = value; return ""; };
    const getCookie = () => cookieValue;
    const apiBaseAddress = new URL("https://try.dot.net");
    const hostOrigin = new URL("https://docs.microsoft.com");
    const encodedHostOrigin = encodeURIComponent(hostOrigin.href);
    const iframeSrc = new URL("https://try.dot.net/v2/editor?hostOrigin=https%3A%2F%2Fdocs.microsoft.com&waitForConfiguration=true");
    const referrer = new URL("https://docs.microsoft.com/en-us/dotnet/api/system.datetime?view=netframework-4.7.1");

    let iframeWindow: ICanAddEventListener<MessageEvent> & IIFrameWindow & ICanSendEvent;

    beforeEach(() => {
        recordedActions = [];
        cookieValue = "";
        recordAction = (a) => recordedActions.push(a);

        hostWindow = {
            recordedMessages: [],
            postMessage: (message, targetOrigin) => {
                hostWindow.recordedMessages.push({
                    message,
                    targetOrigin
                });
            }
        };
        iframeWindow = {
            getApiBaseAddress: () => apiBaseAddress,
            getHostOrigin: () => hostOrigin,
            getReferrer: () => referrer,
            getQuery: () => iframeSrc.searchParams,
            addEventListener: (type, handler) => {
                type.should.equal("message");

                iframeWindow.sendMessageEvent = (origin, data) => {
                    handler(new MessageEvent(type, {
                        origin,
                        data
                    }));
                };
            },
            recordedEvents: [],
            sendMessageEvent: () => { },
            getClientParameters: () => { return {}; }
        };
    });

    it("forwards translated host messages if ?hostOrigin=... is specified and matches", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(emptyWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs"),
            actions.runCodeResultSpecified(
                ["hello", "world"],
                false
            )
        ];

        enzyme.mount(
            <MemoryRouter initialEntries={[`?hostOrigin=${encodedHostOrigin}&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    getCookie={getCookie}
                    setCookie={setCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    hostWindow={hostWindow}
                    iframeWindow={iframeWindow} />
            </MemoryRouter>);

        iframeWindow.sendMessageEvent(hostOrigin.href, {
            type: "setRunResult",
            output: ["hello", "world"],
            succeeded: false
        });

        recordedActions.should.deep.equal(expectedActions);
    });

    it("forwards setSourceCode message", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(emptyWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs"),
            actions.updateWorkspaceBuffer("Console.WriteLine(\"something new\");", "Program.cs")
        ];

        enzyme.mount(
            <MemoryRouter initialEntries={[`?hostOrigin=${encodedHostOrigin}&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    getCookie={getCookie}
                    setCookie={setCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    hostWindow={hostWindow}
                    iframeWindow={iframeWindow} />
            </MemoryRouter>);

        iframeWindow.sendMessageEvent(hostOrigin.href, {
            type: "setSourceCode",
            sourceCode: "Console.WriteLine(\"something new\");"
        });

        recordedActions.should.deep.equal(expectedActions);
    });

    it("does not forward host messages if ?hostOrigin=... is specified but does not match", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(emptyWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        enzyme.mount(
            <MemoryRouter initialEntries={[`?hostOrigin=${encodedHostOrigin}&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    getCookie={getCookie}
                    setCookie={setCookie}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow}
                    aiFactory={(_str) => new NullAIClient} />
            </MemoryRouter>);

        iframeWindow.sendMessageEvent("https://docs.not-microsoft.com/", actions.runClicked());

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches actions to the hostWindow", () => {
        const expectedActions = [{
            editorId: "",
            messageOrigin: `/?hostOrigin=${encodedHostOrigin}`,
            type: "HostListenerReady"
        }, {
            editorId: "",
            messageOrigin: "/?hostOrigin=https%3A%2F%2Fdocs.microsoft.com%2F",
            type: "HostEditorReady"
        },
        {
            editorId: "",
            messageOrigin: "/?hostOrigin=https%3A%2F%2Fdocs.microsoft.com%2F",
            type: "HostRunReady"
        }];

        enzyme.mount(
            <MemoryRouter initialEntries={[`?hostOrigin=${encodedHostOrigin}`]}>
                <BrowserAdapter hostWindow={hostWindow}
                    getCookie={getCookie}
                    setCookie={setCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow} />
            </MemoryRouter>);

        var result = hostWindow.recordedMessages
            .filter((e: { targetOrigin: string }) => e.targetOrigin === hostOrigin.href)
            .map((e: { message: string }) => e.message);

        result.should.deep.equal(expectedActions);
    });

    it("does not log if 'debug' is not specified", () => {
        enzyme.mount(
            <MemoryRouter>
                <BrowserAdapter log={recordAction}
                    getCookie={getCookie}
                    setCookie={setCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    hostWindow={hostWindow}
                    iframeWindow={iframeWindow} />
            </MemoryRouter>);

        expect(recordedActions).to.be.empty;
    });

    it("hides the editor at startup when ?waitForConfiguration=true", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(emptyWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.hideEditor(),
            actions.disableBranding(),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        enzyme.mount(
            <MemoryRouter initialEntries={[`?waitForConfiguration=true&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    getCookie={getCookie}
                    setCookie={setCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    hostWindow={hostWindow}
                    iframeWindow={iframeWindow} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });
});

