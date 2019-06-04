// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";

import { Output } from "../../../src/components/Output";

import {suite} from "mocha-typescript";
import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
import { mount } from "enzyme";
enzyme.configure({ adapter: new Adapter() });
suite("{ Output }", () => {
    it("displays outputLines", () => {
        const wrapper = mount(<Output consoleOutput={["hello", "world"]} />);
        wrapper.text().should.contain("hello")
            .and.contain("world");
    });
    it("appends Exception with preamble to outputLines", () => {
        const wrapper = mount(<Output consoleOutput={["hello", "world"]} exception={"Oopsies"} />);
        wrapper.text().should.contain("hello")
            .and.contain("world")
            .and.contain("Unhandled Exception: Oopsies");
    });
});
