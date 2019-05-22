// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../../src/constants/ActionTypes";

import actions from "../../src/actionCreators/actions";
import { ICodeEditorForTryDotNet } from "../../src/constants/ICodeEditorForTryDotNet";

describe("NOTIFY Action Creators", () => {
    it("should set hosting domain", () => {
        const expectedAction = {
            type: types.NOTIFY_HOST_PROVIDED_CONFIGURATION,
            configuration: { hostOrigin: new URL("http://myDomain") }
        };

        actions.notifyHostProvidedConfiguration({ hostOrigin: new URL("http://myDomain") }).should.deep.equal(expectedAction);
    });

    it("should create an action to notify the host listener that the editor is ready for interaction", () => {
        const expectedAction = {
            type: types.NOTIFY_HOST_LISTENER_READY,
            editorId: "listenerA"
        };

        actions.hostListenerReady("listenerA")
            .should.deep.equal(expectedAction);
    });

    it("should create an action to notify the host listener that the monaco editor is ready.", () => {
        const monacoEditor: ICodeEditorForTryDotNet = {
            focus: () => { },
            layout: () => { }
        };

        const expectedAction = {
            type: types.NOTIFY_MONACO_READY,
            editor: monacoEditor
        };

        actions.notifyMonacoReady(monacoEditor)
            .should.deep.equal(expectedAction);
    });
});
