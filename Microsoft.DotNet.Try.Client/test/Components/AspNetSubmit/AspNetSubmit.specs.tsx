// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// import "ignore-styles";
import * as React from "react";
import { expect, should } from "chai";
import { AspNetSubmit } from "../../../src/components/AspNetSubmit";
import { suite } from "mocha-typescript";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount, shallow } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

should();

suite("{ AspNetSubmit }", () => {
    it("its text reads `Run`", () => {
        const wrapper = shallow(<AspNetSubmit />);
        wrapper.find("button").text().should.equal("Run");
    });
    it("it invokes onClick when clicked", () => {
        var clicked: boolean;
        const wrapper = mount(<AspNetSubmit onClick={() => { clicked = true; } } />);
        wrapper.find("button").simulate("click");
        clicked.should.equal(true);
    });
    it(`it doesn't invoke onClick when clicked while disabled`, () => {
        var clicked = false;
        const wrapper = mount(<AspNetSubmit disabled onClick={() => { clicked = true; } } />);
        wrapper.find("button").simulate("click");
        clicked.should.equal(false);
    });
    it("forwards the http request to client", () => {
        const parameters = {
            httpRequest: {
                uri: "/some/relative/uri",
                verb: "post",
                body: "{ some: \"json\"}"
            }
        };
        let capturedRequest = null;
        const wrapper = mount(<AspNetSubmit disabled={false} onClick={(r) => { capturedRequest = r; } } />);
        let urlInput = wrapper.find("#uriInput");
        urlInput.simulate("change", { target: { value: parameters.httpRequest.uri } });
        let verbInput = wrapper.find("#verbInput");
        verbInput.simulate("change", { target: { value: parameters.httpRequest.verb } });
        let bodyInput = wrapper.find("#bodyInput");
        bodyInput.simulate("change", { target: { value: parameters.httpRequest.body } });
        wrapper.find("button").simulate("click");
        expect(capturedRequest).not.to.be.null;
        expect(capturedRequest).to.deep.equal(parameters);
    });
});

