// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { tryDotNetModes, createRunOutputElements, PreOutputPanel } from "../src/index";
import { JSDOM } from "jsdom";

chai.should();

describe("dom utilities", () => {
    // TODO: move this to integration tests
    describe("can create output panels from a div", () => {

        it("even when not specified the type", () => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <div data-trydotnet-mode="runResult" data-trydotnet-session-id="a"></div>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            let doc = dom.window.document;
            let outputPanelContainer = doc.querySelector<HTMLDivElement>(`div[data-trydotnet-mode=${tryDotNetModes[tryDotNetModes.runResult]}]`);
            let { outputPanel } = createRunOutputElements(outputPanelContainer, doc);

            outputPanel.should.not.be.null;
            outputPanel.constructor.name.should.be.equal(PreOutputPanel.name);
        });
    });
});
