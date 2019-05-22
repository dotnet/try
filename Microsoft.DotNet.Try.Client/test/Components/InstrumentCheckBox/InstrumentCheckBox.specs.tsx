// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as React from "react";
import { InstrumentCheckBox } from "../../../src/components/InstrumentCheckBox";
import * as enzyme from "enzyme";
import * as Adapter from "enzyme-adapter-react-16";
enzyme.configure({ adapter: new Adapter() });

describe("{ InstrumentCheckBox }", () => {
    it("does not render when visible is false", () => {
        const wrapper = enzyme.shallow(<InstrumentCheckBox visible={false} checked={false} />);
        wrapper.isEmptyRender().should.equal(true);
    });
    it("calls onChanged when changed", () => {
        var changed = false;
        const wrapper = enzyme.shallow(<InstrumentCheckBox visible={true} checked={false} onChanged={() => { changed = true; } } />);
        wrapper.find("input").simulate("change");
        changed.should.equal(true);
    });
});
