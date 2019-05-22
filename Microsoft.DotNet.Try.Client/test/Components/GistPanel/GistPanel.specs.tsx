// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";
import * as React from "react";
import { GistPanel } from "../../../src/components/GistPanel";
import { mount } from "enzyme";
import { should } from "chai";
import { expect } from "chai";
import { suite } from "mocha-typescript";
require("jsdom-global")();

should();

suite("{ GistPanel }", () => {
    it("is not rendered as default", () => {
        const wrapper = mount(<GistPanel />);
        expect(wrapper.html()).to.be.null;
    });

    it("is rendered with the urls pointing to gist and raw file", () => {
        const wrapper = mount(<GistPanel showPanel={true} htmlUrl={"gistUrl#file-fileA-cs"} rawUrl={"fileOneUrl"} />);

        wrapper
            .find("#rawFileUrl")
            .props().href.should.be.equal("fileOneUrl");

        wrapper
            .find("#webUrl")
            .props().href.should.be.equal("gistUrl#file-fileA-cs");
    });
});
