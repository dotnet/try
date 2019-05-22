// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as types from "../../src/constants/ActionTypes";
import getStore, { IObservableAppStore } from "../observableAppStore";
import actions from "../../src/actionCreators/actions";
import { nextInstrumentationStep } from "../../src/actionCreators/instrumentationActionCreators";

describe("Instrumentation action creators", () => {
    var store: IObservableAppStore;

    beforeEach(() => {
        store = getStore();
    });

    describe("nextInstrumentationStep()", () => {
        it("Should dispatch NEXT_INSTRUMENTATION_STEP and OUTPUT_UPDATED and CAN_MOVE_NEXT in nextInstrumentationStep", () => {
            store.dispatch(actions.nextInstrumentationStep(() => ["expected"]));
            store.getActions().should.deep.members([
                { type: types.NEXT_INSTRUMENT_STEP },
                actions.outputUpdated(["expected"]),
                actions.canMoveNext()
            ]);
        });

        it("Shouldn't do anything if cannot move next", () => {
            store.dispatch(actions.nextInstrumentationStep(() => Error("nope")));
            store.getActions().should.not.include(
                { type: types.NEXT_INSTRUMENT_STEP }
            );
        });
    });

    describe("previousInstrumentationStep", () => {
        it("Should dispatch PREV_INSTRUMENTATION_STEP and OUTPUT_UPDATED in prevInstrumentationStep", () => {
            store.dispatch(actions.prevInstrumentationStep(() => ["expected"]));
            store.getActions().should.deep.members([
                { type: types.PREV_INSTRUMENT_STEP },
                actions.outputUpdated(["expected"]),
                actions.canMovePrev()
            ]);
        });

        it("Shouldn't do anything if cannot move back", () => {
            store.dispatch(actions.prevInstrumentationStep(() => Error("nope")));
            store.getActions().should.not.include(
                { type: types.PREV_INSTRUMENT_STEP }
            );
        });
    });

    describe("checkPrevEnabled", () => {
        it("Should dispatch CAN_MOVE_PREV if can move back", () => {
            store.configure([actions.runSuccess({
                output: ["before after"],
                instrumentation: [
                    {},
                    {
                        output: {
                            start: 0,
                            end: 6
                        }
                    }]
            })]);
            store.dispatch(nextInstrumentationStep(() => []));
            store.dispatch(actions.checkPrevEnabled());
            store.getActions().should.deep.include({ type: types.CAN_MOVE_PREV });
        });
        it("Should dispatch CANNOT_MOVE_PREV if cannot go back", () => {
            store.configure([actions.runSuccess({
                output: ["before after"],
                instrumentation: [
                    {},
                    {
                        output: {
                            start: 0,
                            end: 6
                        }
                    }]
            })]);
            store.dispatch(actions.checkPrevEnabled());
            store.getActions().should.deep.members([{ type: types.CANNOT_MOVE_PREV }]);
        });
    });

    describe("checkNextEnabled", () => {
        it("Should dispatch CAN_GO_NEXT if can go next", () => {
            store.configure([actions.runSuccess({
                output: ["before after"],
                instrumentation: [
                    {},
                    {
                        output: {
                            start: 0,
                            end: 6
                        }
                    }]
            })]);
            store.dispatch(actions.checkNextEnabled());
            store.getActions().should.deep.equal([{ type: types.CAN_MOVE_NEXT }]);
        });
        it("Should dispatch CANNOT_GO_NEXT if cannot go next", () => {
            store.configure([actions.runSuccess({
                output: ["before after"],
                instrumentation: [
                    {
                        output: {
                            start: 0,
                            end: 6
                        }
                    }]
            })]);
            store.dispatch(actions.checkNextEnabled());
            store.getActions().should.deep.members([{ type: types.CANNOT_MOVE_NEXT }]);
        });
    });

    it("Should calculate standard output for instrumentation step correctly", () => {
        store.configure([actions.runSuccess({
            output: ["before after"],
            instrumentation: [
                {},
                {
                    output: {
                        start: 0,
                        end: 6
                    }
                }]
        })]);

        store.dispatch(actions.nextInstrumentationStep());
        store.getActions().should.deep.include(
            actions.outputUpdated(["before"])
        );
    });
});
