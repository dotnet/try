// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Store, AnyAction } from "redux";
import { ThunkDispatch } from "redux-thunk";
import IState from "./IState";

export type IStore = Store<IState, AnyAction> & { dispatch: ThunkDispatch<IState, null, AnyAction> };
