// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, configureEmbeddableEditorIFrame } from "../src/index";
import { buildSimpleIFrameDom } from "./domUtilities";
import { JSDOM } from "jsdom";
import { Done } from "mocha";
import { ApiMessage, RUN_RESPONSE, RUN_REQUEST, SERVICE_ERROR_RESPONSE, } from "../src/apiMessages";
import { registerForRunRequest, registerForLongRunRequest, notifyRunReadyWithId, registerForOpenProject } from "./messagingMocks";
import { createReadySession } from "./sessionFactory";
import * as polyglotNotebooks from "@microsoft/polyglot-notebooks";
chai.should();

describe("a user", () => {

    describe("with a trydotnet session", () => {

        let configuration: Configuration;
        let dom: JSDOM;
        let editorIFrame: HTMLIFrameElement;

        beforeEach(() => {
            configuration = { hostOrigin: "https://learn.microsoft.com" };
            dom = buildSimpleIFrameDom(configuration);
            let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
            editorIFrame = configureEmbeddableEditorIFrame(iframe, configuration);
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

                registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                    return files.map(f => {
                        let item: polyglotNotebooks.ProjectItem = {
                            relativeFilePath: f.relativeFilePath,
                            regionNames: [],
                            regionsContent: {}
                        };
                        return item;
                    });
                });


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
            let count = 1;
            let results = ["", ""];

            awaitableSession.then(session => {
                registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                    return files.map(f => {
                        let item: polyglotNotebooks.ProjectItem = {
                            relativeFilePath: f.relativeFilePath,
                            regionNames: [],
                            regionsContent: {}
                        };
                        return item;
                    });
                });


                registerForLongRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    count++;
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success",
                        output: [`${count}`]
                    };
                });

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });

                session.run().then(result => {
                    results;//? 
                    if (result.succeeded) {
                        results[0] = result.output![0]; //?
                        if (results[0] === results[1]) {
                            done();
                        }
                    } else {

                    }
                });

                session.run().then(result => {
                    results;//? 
                    if (result.succeeded) {
                        results[1] = result.output![0]; //?
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
                registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                    return files.map(f => {
                        let item: polyglotNotebooks.ProjectItem = {
                            relativeFilePath: f.relativeFilePath,
                            regionNames: [],
                            regionsContent: {}
                        };
                        return item;
                    });
                });

                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success",
                        output: ["line one", "line two"]
                    }
                });


                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });

                let subscriptions = session.subscribeToOutputEvents((event) => {
                    event.stdout!.should.be.deep.equal(["line one", "line two"]);
                    done();
                });
                session.run();
            });
        });

        it("can run the loaded project and intercept service error", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {

                registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                    return files.map(f => {
                        let item: polyglotNotebooks.ProjectItem = {
                            relativeFilePath: f.relativeFilePath,
                            regionNames: [],
                            regionsContent: {}
                        };
                        return item;
                    });
                });

                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: SERVICE_ERROR_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        message: "failed to run",
                        statusCode: "503"
                    }
                });


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

                registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                    return files.map(f => {
                        let item: polyglotNotebooks.ProjectItem = {
                            relativeFilePath: f.relativeFilePath,
                            regionNames: [],
                            regionsContent: {}
                        };
                        return item;
                    });
                });


                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: request.type === RUN_REQUEST && request.parameters!["instrument"] ? "Success" : "Exception"
                    }
                });


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
                    if (val) {
                        done()
                    }
                })

            });

            notifyRunReadyWithId(configuration, dom.window, "0");

        });

        it("can run the loaded project with workflow id", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {
                registerForOpenProject(configuration, editorIFrame, dom.window, (files) => {
                    return files.map(f => {
                        let item: polyglotNotebooks.ProjectItem = {
                            relativeFilePath: f.relativeFilePath,
                            regionNames: [],
                            regionsContent: {}
                        };
                        return item;
                    });
                });


                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: request.type === RUN_REQUEST && request.parameters!["runWorkflowId"] === "webApi" ? "Success" : "Exception"
                    }
                });


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
