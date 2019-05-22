// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import actions from "../../src/actionCreators/actions";
import reducer from "../../src/reducers/workspaceInfoReducer";

describe("workspaceInfo Reducer", () => {
    const defaultWorkspaceInfo = { originType: "undefinedOrigin" };
    it("should return the initial state", () => {
        reducer(undefined, undefined).should.deep.equal(defaultWorkspaceInfo);
    });

    it("reacts to set workspace info", () => {
        const action = actions.setWorkspaceInfo({originType: "gist"});

        reducer({ ...defaultWorkspaceInfo }, action).originType.should.equal("gist");
    });
});
