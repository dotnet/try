// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

require("jsdom-global")();

import * as React from "react";

import { RunButton } from "../../../src/components/RunButton";
import { should } from "chai";
import { suite } from "mocha-typescript";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount, shallow } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

should();

suite("{ RunButton }", () => {
    it("its text reads `Run`", () => {
        const wrapper = shallow(<RunButton />);
        wrapper.find("button").text().should.equal("Run");
    });
    it("it invokes onClick when clicked", () => {
        var clicked: boolean;
        const wrapper = mount(<RunButton onClick={() => { clicked = true; } } />);
        wrapper.find("button").simulate("click");
        clicked.should.equal(true);
    });
    it(`it doesn't invoke onClick when clicked while disabled`, () => {
        var clicked = false;
        const wrapper = mount(<RunButton disabled onClick={() => { clicked = true; } } />);
        wrapper.find("button").simulate("click");
        clicked.should.equal(false);
    });
});

