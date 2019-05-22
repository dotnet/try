// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { JSDOM } from "jsdom";
import { IFrameWindow } from "../src/components/IFrameWindow";

chai.should();

describe("IFrameWindow", () => {
    it("can return the url of the referrer", () => {
        let dom = new JSDOM(`<html>
            <body>
                <script id="bundlejs"
                        data-client-parameters="{&quot;scaffold&quot;:&quot;Method&quot;,&quot;referrer&quot;:&quot;https://docs.microsoft.com/some/nice/page&quot;}"
                    src="/bundle.js?v=1.0.0.0">
                </script>
            </body>
            </html>`, {
                url: new URL("https://try.dot.net/v2/editor").href
            });

        let iframeWindow = new IFrameWindow(dom.window);

        let referrer = iframeWindow.getReferrer();
        referrer.should.be.deep.equal(new URL("https://docs.microsoft.com/some/nice/page"));
    });

    it("can return the url of the hostOrigin", () => {
        let dom = new JSDOM(`<html>
            <body>
                <script id="bundlejs"
                        data-client-parameters="{&quot;scaffold&quot;:&quot;Method&quot;,&quot;referrer&quot;:&quot;https://docs.microsoft.com/some/nice/page&quot;}"
                    src="/bundle.js?v=1.0.0.0">
                </script>
            </body>
            </html>`, {
                url: new URL("https://try.dot.net/v2/editor").href
            });

        let iframeWindow = new IFrameWindow(dom.window);

        let hostOrigin = iframeWindow.getHostOrigin();
        hostOrigin.should.be.deep.equal(new URL("https://docs.microsoft.com/some/nice/page"));
    });

    it("can return the url for the api base address", () => {
        let dom = new JSDOM(`<html>
            <body>
                <script id="bundlejs"
                        data-client-parameters="{&quot;scaffold&quot;:&quot;Method&quot;,&quot;referrer&quot;:&quot;https://docs.microsoft.com/some/nice/page&quot;}"
                    src="/bundle.js?v=1.0.0.0">
                </script>
            </body>
            </html>`, {
                url: new URL("https://try.dot.net/v2/editor").href
            });

        let iframeWindow = new IFrameWindow(dom.window);

        let apiBaseAddress = iframeWindow.getApiBaseAddress();
        apiBaseAddress.should.be.deep.equal(new URL("https://try.dot.net/"));
    });
});

