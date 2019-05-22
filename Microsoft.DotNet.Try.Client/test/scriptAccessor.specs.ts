// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { JSDOM } from "jsdom";
import { ScriptTagAccessor } from "../src/components/ScriptTagAccessor";

chai.should();

describe("scriptAccessor", () => {
    it("can deserialize client parameters", () => {
        let dom = new JSDOM(`<html>
            <body>
                <script id="bundlejs"
                        data-client-parameters="{&quot;scaffold&quot;:&quot;Method&quot;,&quot;referrer&quot;:&quot;https://docs.microsoft.com/some/nice/page&quot;}"
                    src="/bundle.js?v=1.0.0.0">
                </script>
            </body>
            </html>`);

        let accessor = new ScriptTagAccessor(dom.window.document);

        let parameters = accessor.getClientParameters();

        parameters.referrer.should.be.deep.equal(new URL("https://docs.microsoft.com/some/nice/page"));
    });
});
