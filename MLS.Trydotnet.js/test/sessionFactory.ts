// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ISession, createSession, Configuration } from "../src";
import { notifyEditorReadyWithId } from "./messagingMocks";

export function createReadySession(configuration: Configuration, editorIFrame: HTMLIFrameElement, window: Window): Promise<ISession>{    
    let awaitableSession = createSession(configuration, [editorIFrame], window);
    notifyEditorReadyWithId(configuration, window, "0");
    return awaitableSession;
}

export function createReadySessionWithMultipleEditors(configuration: Configuration, editorIFrames: HTMLIFrameElement[], window: Window): Promise<ISession>{    
    let awaitableSession = createSession(configuration, editorIFrames, window);
    for(let editorIframe of editorIFrames){
        notifyEditorReadyWithId(configuration, window, editorIframe.dataset.trydotnetEditorId);
    }    
    return awaitableSession;
}