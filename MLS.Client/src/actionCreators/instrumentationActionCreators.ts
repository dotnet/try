// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../constants/ActionTypes";
import IState from "../IState";
import { Dispatch } from "redux";
import { outputUpdated } from "./runActionCreators";
import { Action } from "../constants/ActionTypes";

type outputCalculator = (modifier: number, state: IState) => string[] | Error;

export const nextInstrumentationStep = (calculateOutput: outputCalculator = calculateOutputForStep) => (dispatch: Dispatch, getState: () => IState) => {
    const state = getState();
    const newOutput = calculateOutput(1, state);
    const atEnd = calculateOutput(2, state) instanceof Error;

    if (newOutput instanceof Error) {
        dispatch(cannotMoveNext());
    } else {
        dispatch({ type: types.NEXT_INSTRUMENT_STEP });
        dispatch(outputUpdated(newOutput));

        if (atEnd) {
            dispatch(cannotMoveNext());
        } else {
            dispatch(canMoveNext());
        }
    }
};

export const prevInstrumentationStep = (calculateOutput: outputCalculator = calculateOutputForStep) => (dispatch: Dispatch, getState: () => IState) => {
    const state = getState();
    const newOutput = calculateOutput(-1, state);
    const atEnd = calculateOutput(-2, state) instanceof Error;

    if (newOutput instanceof Error) {
        dispatch(cannotMovePrev());
    } else {
        dispatch({ type: types.PREV_INSTRUMENT_STEP });
        dispatch(outputUpdated(newOutput));
        if (atEnd) {
            dispatch(cannotMovePrev());
        } else {
            dispatch(canMovePrev());
        }
    }
};

export const checkNextEnabled = () => (dispatch: Dispatch, getState: () => IState) => {
    const state = getState();
    if (canMoveInstrumentation(state, 1)) {
        dispatch(canMoveNext());
    } else {
        dispatch(cannotMoveNext());
    }
};

export const checkPrevEnabled = () => (dispatch: Dispatch, getState: () => IState) => {
    const state = getState();
    if (canMoveInstrumentation(state, -1)) {
        dispatch(canMovePrev());
    } else {
        dispatch(cannotMovePrev());
    }
};

export const calculateOutputForStep = (modifier: number, state: IState): string[] | Error => {
    const nextIndexInvalid = !canMoveInstrumentation(state, modifier);
    if (nextIndexInvalid) {
        return Error("Out of bounds");
    }

    const currentStep = state.run.instrumentation[state.run.currentInstrumentationStep + modifier];

    const rawOutput = state.run.fullOutput.join("\n") || "";

    const output = rawOutput.substring(0, currentStep.output.end);
    return output.split("\n");
};

function canMoveInstrumentation(state: IState, modifier: number): boolean {
    if (!state.run.instrumentation || state.run.instrumentation.length === 0) {
        return false;
    }

    const nextIndex = state.run.currentInstrumentationStep + modifier;
    const nextIndexValid = !(nextIndex < 0 || nextIndex >= state.run.instrumentation.length);
    return nextIndexValid;
}

export function cannotMoveNext(): Action {
    return {
        type: types.CANNOT_MOVE_NEXT
    };
}

export function canMoveNext(): Action {
    return {
        type: types.CAN_MOVE_NEXT
    };
}

export function cannotMovePrev(): Action {
    return {
        type: types.CANNOT_MOVE_PREV
    };
}

export function canMovePrev(): Action {
    return {
        type: types.CAN_MOVE_PREV
    };
}
