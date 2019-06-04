// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";

import DotDotNetCodeRunner from "../../../src/components/DotDotNetCodeRunner";
import Editor from "../../../src/components/Editor";
import Output from "../../../src/components/Output";
import RunButton from "../../../src/components/RunButton";
import Running from "../../../src/components/Running";
import { shallow } from "enzyme";
import { JSDOM } from "jsdom";
import TryDotnetBanner from "../../../src/components/TryDotnetBanner";

describe("DotDotNetCodeRunner(dom)", () => {
    let url = new URL("http://try.dot.net");
    url.searchParams.append("hostOrigin", "http://foo.com");
    let dom = new JSDOM(`<!DOCTYPE html>
    <html lang="en">
    <body>
        <pre height="300px" width="800px" data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="preSession">
    using System;
    public class Program {
        public static void Main()
        {
            Console.WriteLine("yes in pre tag");
        }
    }
        </pre>
    </body>
    </html>`,
    {
        url: url.toString(),
        runScripts: "dangerously"
    });


    it("contains an <Editor /> component", () => {
        const wrapper = shallow(DotDotNetCodeRunner(dom.window));

        wrapper.find(Editor).should.have.length(1);
    });

    it("contains a <RunButton /> component", () => {
        const wrapper = shallow(DotDotNetCodeRunner(dom.window));

        wrapper.find(RunButton).should.have.length(1);
    });

    it("contains an <Output /> component", () => {
        const wrapper = shallow(DotDotNetCodeRunner(dom.window));

        wrapper.find(Output).should.have.length(1);
    });

    it("contains an <Running /> component", () => {
        const wrapper = shallow(DotDotNetCodeRunner(dom.window));

        wrapper.find(Running).should.have.length(1);
    });

    it("contains a <TryDotnetBanner /> component", () => {
        const wrapper = shallow(DotDotNetCodeRunner(dom.window));

        wrapper.find(TryDotnetBanner).should.have.length(1);
    });

    it("contains a <Frame /> component", () => {
        const wrapper = shallow(DotDotNetCodeRunner(dom.window));
        wrapper.find("Connect(Frame)").should.have.length(1);
    });
});
