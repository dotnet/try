// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DOMWindow } from "jsdom";
import { ISession, createSession, Configuration } from "../src";
import { notifyEditorReadyWithId } from "./messagingMocks";

export function createReadySession(configuration: Configuration, editorIFrame: HTMLIFrameElement, window: DOMWindow): Promise<ISession>{    
    let awaitableSession = createSession(configuration, [editorIFrame], <Window><any>window);
    notifyEditorReadyWithId(configuration, window, "0");
    return awaitableSession;
}

export function createReadySessionWithMultipleEditors(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: DOMWindow): Promise<ISession>{    
    let awaitableSession = createSession(configuration, editorIFrames, <Window><any>window);
    for(let editorIframe of editorIFrames){
        notifyEditorReadyWithId(configuration, window, editorIframe.dataset.trydotnetEditorId);
    }    
    return awaitableSession;
}