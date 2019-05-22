// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { RunResult } from "./session";
import { IOutputPanel } from "./outputPanel";
import { ITextDisplay } from "./textDisplay";

export interface IInstrumentationFrameNavigator{
    canMoveToNextFrame: boolean;
    caMoveTPreviousFrame: boolean;
    moveToNextFrame(): void;
    moveTPreviousFrame(): void;
}

export function createInstrumentationFrameNavigator(runResult: RunResult, editor:ITextDisplay, outputPanel: IOutputPanel): IInstrumentationFrameNavigator{
    throw new Error("not implemented")
}