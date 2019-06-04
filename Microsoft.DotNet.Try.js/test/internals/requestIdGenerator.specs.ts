// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { buildSimpleIFrameDom } from "../domUtilities";
import * as trydotnet from "../../src/index";
import { IFrameMessageBus } from "../../src/internals/messageBus";
import { configureEmbeddableEditorIFrame } from "../../src/htmlDomHelpers";
import { RequestIdGenerator } from "../../src/internals/requestIdGenerator";
import { registerForRequestIdGeneration } from "../messagingMocks";

chai.should();

describe("a request id generator", () => {

    const defaultConfiguration: trydotnet.Configuration = { hostOrigin: "https://docs.microsoft.com" };

    it("requests opeartionId from the editor", async () => {
        var dom = buildSimpleIFrameDom(defaultConfiguration);

        let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
        iframe = configureEmbeddableEditorIFrame(iframe, "0", defaultConfiguration);

        registerForRequestIdGeneration(defaultConfiguration, iframe, dom.window, (_rid) => "TestRun");

        let bus = new IFrameMessageBus(iframe, dom.window, "0");

        let generator = new RequestIdGenerator(bus);

        let opId = await generator.getNewRequestId();

        opId.should.equal("TestRun");
    });

    it("uses local generator if cannot communicate with editor", async () => {
        var dom = buildSimpleIFrameDom(defaultConfiguration);

        let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
        iframe = configureEmbeddableEditorIFrame(iframe, "0", defaultConfiguration);

        let bus = new IFrameMessageBus(iframe, dom.window, "0");

        let generator = new RequestIdGenerator(bus, 100);

        let opId = await generator.getNewRequestId();

        opId.should.contain("trydotnetjs.session.");
    });
});