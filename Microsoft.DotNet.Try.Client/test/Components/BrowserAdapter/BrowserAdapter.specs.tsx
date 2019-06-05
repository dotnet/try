// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";

import * as React from "react";

import { defaultWorkspace, emptyWorkspace, fibonacciCode } from "../../testResources";

import BrowserAdapter, { IIFrameWindow } from "../../../src/components/BrowserAdapter";
import { MemoryRouter } from "react-router-dom";
import MlsClient from "../../../src/MlsClient";
import actions from "../../../src/actionCreators/actions";
import { encodeWorkspace } from "../../../src/workspaces";
import fetch from "../../../src/utilities/fetch";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
import { should } from "chai";
import { NullAIClient } from "../../../src/ApplicationInsights";
enzyme.configure({ adapter: new Adapter() });

describe("<BrowserAdapter />", () => {
    var recordedActions: Object[];
    var recordAction: (a: Object) => void;
    const setCookie = () => "";
    const getCookie = () => "";
    const apiBaseAddress = new URL("https://try.dot.net");
    const hostOrigin = new URL("https://docs.microsoft.com");
    const iframeSrc = new URL("https://try.dot.net/v2/editor?hostOrigin=https%3A%2F%2Fdocs.microsoft.com&waitForConfiguration=true");
    const referrer = new URL("https://docs.microsoft.com/en-us/dotnet/api/system.datetime?view=netframework-4.7.1");

    const iframeWindow: IIFrameWindow = {
        getApiBaseAddress: () => apiBaseAddress,
        getHostOrigin: () => hostOrigin,
        getReferrer: () => referrer,
        getQuery: () => iframeSrc.searchParams,
        addEventListener: (_type, _listener) => { },
        getClientParameters: () => ({})
    };

    const hostWindow = {
        postMessage: (_message: Object, _targetOrigin: string) => {

        },
        getQuery: () => new URLSearchParams()
    };

    beforeEach(() => {
        should();
        recordedActions = [];
        recordAction = (a) => recordedActions.push(a);
    });

    it("contains the specified component", () => {
        let SomeComponent = () => (<div>hi!</div>);

        const wrapper = mount(
            <MemoryRouter>
                <BrowserAdapter setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} >
                    <SomeComponent />
                </BrowserAdapter>
            </MemoryRouter>);

        wrapper.find(SomeComponent).should.have.length(1);
    });

    it("dispatches LOAD_SOURCE_REQUEST to the Store", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(emptyWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                iframeWindow.getApiBaseAddress())),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        mount(
            <MemoryRouter initialEntries={[`?debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>
        );

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches `from` query parameter to the Store", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(emptyWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.setCodeSource("http://sample.com/"),
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
            actions.loadCodeRequest("http://sample.com/")
        ];

        var hack = (global: any) => {
            global.fetch = async () => Promise.resolve({});
        };

        hack(global);

        mount(
            <MemoryRouter initialEntries={[`?from=http%3A%2F%2Fsample.com%2F&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>
        );

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches `hostOrigin` query parameter to the Store", () => {
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

        mount(
            <MemoryRouter initialEntries={[`?hostOrigin=http%3A%2F%2Fsample.com%2F&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches `workspaceType` query parameter to the Store", () => {
        const consoleWorkspace = { ...emptyWorkspace, workspaceType: "console" };
        const expectedActions = [
            actions.setWorkspaceType("console"),
            actions.setWorkspace(consoleWorkspace),
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

        mount(
            <MemoryRouter initialEntries={[`?hostOrigin=${encodeURIComponent(hostOrigin.href)}&workspaceType=console&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("initialises code from script workspace", () => {
        const encodedWorkspace = encodeWorkspace(defaultWorkspace);

        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(defaultWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.setCodeSource("workspace"),
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
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        mount(
            <MemoryRouter initialEntries={[`?hostOrigin=${encodeURIComponent(hostOrigin.href)}&workspace=${encodedWorkspace}&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches `completion` query parameter to the Store", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace(emptyWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.setCompletionProvider("pythia"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin: hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        mount(
            <MemoryRouter initialEntries={[`?hostOrigin=http%3A%2F%2Fsample.com%2F&completion=pythia&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches CONFIGURE_ENABLE_PREVIEW to the Store", () => {
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
            actions.enablePreviewFeatures(),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        mount(
            <MemoryRouter initialEntries={[`?preview=true&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>
        );

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches CONFIGURE_ENABLE_INSTRUMENTATION if query flag is set", () => {
        mount(
            <MemoryRouter initialEntries={[`?debug=true&instrument=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>
        );

        recordedActions.should.deep.include(actions.enableInstrumentation());
    });

    it("initialises workspace from gist", () => {
        const expectedActions = [
            actions.setWorkspaceType("script"),
            actions.setWorkspace({ workspaceType: "script", usings: [], files: [], buffers: [{ id: "FibonacciGenerator.cs", content: "", position: 0 }] }),
            actions.setActiveBuffer("FibonacciGenerator.cs"),
            actions.setCodeSource("gist::df44833326fcc575e8169fccb9d41fc7"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin: hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("gist::df44833326fcc575e8169fccb9d41fc7")
        ];

        mount(
            <MemoryRouter initialEntries={[`?hostOrigin=http%3A%2F%2Fsample.com%2F&fromGist=df44833326fcc575e8169fccb9d41fc7&bufferId=FibonacciGenerator.cs&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={iframeWindow}
                    hostWindow={hostWindow} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches `workspaceType` query parameter from parent window to the Store", () => {
        const consoleWorkspace = { ...emptyWorkspace, workspaceType: "somethingCool" };
        const expectedActions = [
            actions.setWorkspaceType("somethingCool"),
            actions.setWorkspace(consoleWorkspace),
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

        mount(
            <MemoryRouter initialEntries={[`?workspaceType=console&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={{ ...iframeWindow, getClientParameters: () => ({ workspaceType: "somethingCool" }) }}
                    hostWindow={{ postMessage: (_, __) => { } }}
                />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches `editorId` query parameter from parent window to the Store", () => {
        const consoleWorkspace = { ...emptyWorkspace, workspaceType: "somethingCool" };
        const expectedActions = [
            actions.setWorkspaceType("somethingCool"),
            actions.setWorkspace(consoleWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady("differentId"),
            actions.hostEditorReady("differentId"),
            actions.hostRunReady("differentId"),
            actions.loadCodeRequest("default"),
            actions.loadCodeSuccess(fibonacciCode, "Program.cs")
        ];

        mount(
            <MemoryRouter initialEntries={[`?workspaceType=console&debug=true&editorId=differentId`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    iframeWindow={{ ...iframeWindow, getClientParameters: () => ({ workspaceType: "somethingCool" }) }}
                    hostWindow={{ postMessage: (_, __) => { } }}
                />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("dispatches `workspaceType` and scaffold query parameter from script tag to the Store", () => {
        const consoleWorkspace = {
            ...emptyWorkspace,
            workspaceType: "somethingCool",
        };

        const scaffoldWorkspace = {
            activeBufferId: "file.cs@scaffold",
            buffers: [
                {
                    content: "",
                    id: "file.cs@scaffold",
                    position: 0
                }
            ],
            files: [
                {
                    name: "file.cs",
                    text: "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nclass C\n{\npublic static void Main()\n    {\n#region scaffold               \n#endregion\n    }                \n}"
                }
            ],
            workspaceType: "somethingCool"
        };
        const expectedActions = [
            actions.setWorkspaceType("somethingCool"),
            actions.setWorkspace(consoleWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setWorkspace(scaffoldWorkspace),
            actions.setActiveBuffer("file.cs@scaffold"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess("", "file.cs@scaffold"),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.hostListenerReady(),
            actions.hostEditorReady(),
            actions.hostRunReady(),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess("", "file.cs@scaffold")
        ];

        var params = new URLSearchParams();
        params.set("workspaceType", "somethingCool");
        params.set("scaffold", "Method");

        mount(
            <MemoryRouter initialEntries={[`?workspaceType=console&debug=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    hostWindow={hostWindow}
                    iframeWindow={{ ...iframeWindow, getClientParameters: () => ({ workspaceType: "somethingCool", scaffold: "Method" }) }} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });

    it("waits for wasmRunner to become ready", () => {
        const consoleWorkspace = {
            ...emptyWorkspace,
            workspaceType: "blazor-console",
        };

        const scaffoldWorkspace = {
            activeBufferId: "file.cs@scaffold",
            buffers: [
                {
                    content: "",
                    id: "file.cs@scaffold",
                    position: 0
                }
            ],
            files: [
                {
                    name: "file.cs",
                    text: "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nclass C\n{\npublic static void Main()\n    {\n#region scaffold               \n#endregion\n    }                \n}"
                }
            ],
            workspaceType: "blazor-console"
        };
        const expectedActions = [
            actions.setWorkspaceType("blazor-console"),
            actions.setWorkspace(consoleWorkspace),
            actions.setActiveBuffer("Program.cs"),
            actions.configureWasmRunner(),
            actions.enableClientTelemetry(new NullAIClient()),
            actions.setWorkspace(scaffoldWorkspace),
            actions.setActiveBuffer("file.cs@scaffold"),
            actions.setCodeSource("workspace"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess("", "file.cs@scaffold"),
            actions.setClient(new MlsClient(fetch,
                hostOrigin,
                getCookie,
                new NullAIClient(),
                apiBaseAddress)),
            actions.notifyHostProvidedConfiguration({ hostOrigin }),
            actions.configureEditorId("foo"),
            actions.hostEditorReady("foo"),
            actions.loadCodeRequest("workspace"),
            actions.loadCodeSuccess("", "file.cs@scaffold")
        ];

        var params = new URLSearchParams();
        params.set("workspaceType", "somethingCool");
        params.set("scaffold", "Method");

        mount(
            <MemoryRouter initialEntries={[`?workspaceType=blazor-console&debug=true&editorId=foo&useWasmRunner=true`]}>
                <BrowserAdapter log={recordAction}
                    setCookie={setCookie}
                    getCookie={getCookie}
                    aiFactory={(_str) => new NullAIClient()}
                    hostWindow={hostWindow}
                    iframeWindow={{ ...iframeWindow, getClientParameters: () => ({ workspaceType: "blazor-console", scaffold: "Method" }) }} />
            </MemoryRouter>);

        recordedActions.should.deep.equal(expectedActions);
    });
});
