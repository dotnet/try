// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, configureEmbeddableEditorIFrame } from "../src/index";
import { buildSimpleIFrameDom } from "./domUtilities";
import { JSDOM } from "jsdom";
import { Done } from "mocha";
import { ApiMessage, RUN_RESPONSE, RUN_REQUEST, SERVICE_ERROR_RESPONSE, } from "../src/internals/apiMessages";
import { registerForRunRequest, registerForRequestIdGeneration, registerForLongRunRequest, notifyEditorReadyWithId, notifyRunReadyWithId } from "./messagingMocks";
import { createReadySession } from "./sessionFactory";

chai.should();

describe("a user", () => {

    describe("with a trydotnet session", () => {

        let configuration: Configuration;
        let dom: JSDOM;
        let editorIFrame: HTMLIFrameElement;

        beforeEach(() => {
            configuration = { hostOrigin: "https://docs.microsoft.com" };
            dom = buildSimpleIFrameDom(configuration);
            let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
            editorIFrame = configureEmbeddableEditorIFrame(iframe, "0", configuration);
        });

        it("can run the loaded project", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {
                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success"
                    };
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });

                session.run().then(result => {
                    if (result.succeeded) {
                        done();
                    } else {

                    }
                });
            });
        });

        it("will get the current run if requesting one while previous one is inflight", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            let count = 0;
            let results = ["", ""];

            awaitableSession.then(session => {
                registerForLongRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    count++;
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success",
                        output: [`${count}`]
                    };
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });

                session.run().then(result => {
                    if (result.succeeded) {
                        results[0] = result.output[0];
                        if (results[0] === results[1]) {
                            done();
                        }
                    } else {

                    }
                });

                session.run().then(result => {
                    if (result.succeeded) {
                        results[1] = result.output[0];
                        if (results[0] === results[1]) {
                            done();
                        }
                    } else {

                    }
                });
            });
        });

        it("can subscribe to output events", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {
                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success",
                        output: ["line one", "line two"]
                    }
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });

                let subscriptions = session.subscribeToOutputEvents((event) => {
                    event.stdout.should.be.deep.equal(["line one", "line two"]);
                    done();
                });
                session.run();
            });
        });

        it("can run the loaded project and intercept service error", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {
                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: SERVICE_ERROR_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        message: "failed to run",
                        statusCode: "503"
                    }
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });

                session.run().catch(error => {
                    if (error.message === "failed to run") {
                        done();
                    }
                });
            });
        });

        it("can run the loaded project as instrumented", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {
                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: request.type === RUN_REQUEST && request.parameters["instrument"] ? "Success" : "Exception"
                    }
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });
                session.run({ instrument: true }).then(result => {
                    if (result.succeeded) {
                        done();
                    }
                });
            });
        });

        it("waits for run ready notification", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);

            awaitableSession.then(session => {
                session.onCanRunChanged(val => {
                    if (val)
                    {
                        done()
                    }
                })

            });

            notifyRunReadyWithId(configuration, dom.window, "0");

        });

        it("can run the loaded project with workflow id", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {
                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: request.type === RUN_REQUEST && request.parameters["runWorkflowId"] === "webApi" ? "Success" : "Exception"
                    }
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });
                session.run({ runWorkflowId: "webApi" }).then(result => {
                    if (result.succeeded) {
                        done();
                    }
                });
            });       
        });
    });
});
