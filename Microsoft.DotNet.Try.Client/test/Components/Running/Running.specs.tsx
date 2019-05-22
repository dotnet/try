// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";

import { Running } from "../../../src/components/Running";

import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { shallow } from "enzyme";
enzyme.configure({ adapter: new Adapter() });

describe("{ Running }", () => {
    it(`displays 'running' when visible is true`, () => {
        const wrapper = shallow(<Running visible />);
        wrapper.find("div").text().should.equal("running...");
    });

    it(`does not display 'running' when visible is false`, () => {
        const wrapper = shallow(<Running />);
        wrapper.find("div").length.should.equal(0);
    });
});
