// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Done } from "mocha";
import { buildSimpleIFrameDom } from "../domUtilities";
import * as trydotnet from "../../src/index";
import { IFrameMessageBus } from "../../src/internals/messageBus";
import { RUN_REQUEST, RUN_RESPONSE, HOST_EDITOR_READY_EVENT } from "../../src/internals/apiMessages";
import { configureEmbeddableEditorIFrame } from "../../src/htmlDomHelpers";

chai.should();

describe("a message bus", () => {

    const defaultConfiguration: trydotnet.Configuration = { hostOrigin: "https://docs.microsoft.com" };

    it("can post message to iframe", (done: Done) => {
        var dom = buildSimpleIFrameDom(defaultConfiguration);

        let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
        iframe = configureEmbeddableEditorIFrame(iframe, "0", defaultConfiguration);

        iframe.contentWindow.addEventListener("message", (message: any) => {
            if (message.data.type === RUN_REQUEST) {
                done();
            }
        });

        let bus = new IFrameMessageBus(iframe, dom.window, "0");

        bus.post({ type: RUN_REQUEST, requestId: "0" });
    });

    it("can receive messages from the main window", (done: Done) => {
        var dom = buildSimpleIFrameDom(defaultConfiguration);;

        let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
        iframe = configureEmbeddableEditorIFrame(iframe, "0", defaultConfiguration);

        dom.window.postMessage({ type: RUN_RESPONSE }, defaultConfiguration.hostOrigin);

        let bus = new IFrameMessageBus(iframe, dom.window, "0");

        bus.subscribe((message) => {
            done();
        });
    });

    it("can receive messages from the main window with matching editorId", (done: Done) => {
        var dom = buildSimpleIFrameDom(defaultConfiguration);;

        let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
        iframe = configureEmbeddableEditorIFrame(iframe, "0", defaultConfiguration);

        dom.window.postMessage({ type: HOST_EDITOR_READY_EVENT, editorId: "0" }, defaultConfiguration.hostOrigin);

        let bus = new IFrameMessageBus(iframe, dom.window, "0");

        bus.subscribe((message) => {
            done();
        });
    });
});