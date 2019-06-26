// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";

import Editor from "../../../src/components/Editor";
import MsDocsCodeRunner from "../../../src/components/MsDocsCodeRunner";
import Output from "../../../src/components/Output";
import RunButton from "../../../src/components/RunButton";
import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { shallow } from "enzyme";
import { JSDOM } from "jsdom";
import Frame from "../../../src/components/Frame";
import * as chai from "chai";
import TryDotnetBanner from "../../../src/components/TryDotnetBanner";
chai.should();

enzyme.configure({ adapter: new Adapter() });

describe("<MsDocsCodeRunner />", () => {
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
        const wrapper = shallow(MsDocsCodeRunner(dom.window));

        wrapper.find(Editor).should.have.length(1);
    });

    it("contains an <Editor /> component", () => {
        const wrapper = shallow(MsDocsCodeRunner(dom.window));

        let frames = wrapper.find(Frame);
        frames.should.have.length(1);
        let frame = frames.first();
        let href = frame.getElement().props.src.href;
        href.should.equal("http://try.dot.net/LocalCodeRunner/blazor-console/?embeddingHostOrigin=http%3A%2F%2Ffoo.com&trydotnetHostOrigin=http%3A%2F%2Ftry.dot.net");
    });

    it("does not contain a <RunButton /> component", () => {
        const wrapper = shallow(MsDocsCodeRunner(dom.window));

        wrapper.find(RunButton).should.have.length(0);
    });

    it("does not contain an <Output /> component", () => {
        const wrapper = shallow(MsDocsCodeRunner(dom.window));

        wrapper.find(Output).should.have.length(0);
    });

    it("contains a <TryDotnetBanner /> component", () => {
        const wrapper = shallow(MsDocsCodeRunner(dom.window));

        wrapper.find(TryDotnetBanner).should.have.length(1);
    });

    it.only("The frame component has the workspace set from the client parameters", () => {
        var workspaceType = "my_workspace_type";
        let dom = new JSDOM(`<!DOCTYPE html>
    <html lang="en">
    <body>
        <script id="bundlejs" data-client-parameters="{&quot;workspaceType&quot;:&quot;${workspaceType}&quot;}"></script>
    </body>
    </html>`,
            {
                url: url.toString(),
                runScripts: "dangerously"
            });

        const wrapper = shallow(MsDocsCodeRunner(dom.window));
        let frames = wrapper.find(Frame);
        let frame = frames.first();
        let href = frame.getElement().props.src.href;
        href.should.equal(`http://try.dot.net/LocalCodeRunner/${workspaceType}/?embeddingHostOrigin=http%3A%2F%2Ffoo.com&trydotnetHostOrigin=http%3A%2F%2Ftry.dot.net`);
    });
});
