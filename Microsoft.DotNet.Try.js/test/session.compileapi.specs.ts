// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { Configuration, configureEmbeddableEditorIFrame, createProject } from "../src/index";
import { buildSimpleIFrameDom } from "./domUtilities";
import { JSDOM } from "jsdom";
import { registerForCompileRequest, registerForRequestIdGeneration, notifyEditorReady } from "./messagingMocks";
import { ApiMessage, COMPILE_RESPONSE, COMPILE_REQUEST, SERVICE_ERROR_RESPONSE } from "../src/internals/apiMessages";
import { createReadySession } from "./sessionFactory";
import { Done } from "mocha";
chai.should();

describe("a user", () => {

    let configuration: Configuration;
    let dom: JSDOM;
    let editorIFrame: HTMLIFrameElement;

    beforeEach(() => {
        configuration = { hostOrigin: "https://docs.microsoft.com" };
        dom = buildSimpleIFrameDom(configuration);
        let iframe = <HTMLIFrameElement>(dom.window.document.querySelector("iframe"));
        editorIFrame = configureEmbeddableEditorIFrame(iframe, "0", configuration);
    });

    describe("with a trydotnet session", () => {
        it("can compile the loaded project", async () => {
            let session = await createReadySession(configuration, editorIFrame, dom.window);

            registerForCompileRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                return {
                    type: COMPILE_RESPONSE,
                    requestId: request.type === COMPILE_REQUEST ? request.requestId : undefined,
                    outcome: request.type === COMPILE_REQUEST ? "Success" : "CompilationError"
                }
            });

            registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

            let project = await createProject("console", [{ name: "program.cs", content: "" }]);
            session.openProject(project);

            let result = await session.compile();
            result.succeeded.should.be.true;
        });

        it("can compile the loaded project and intercept service error", (done: Done) => {
            let awaitableSession = createReadySession(configuration, editorIFrame, dom.window);
            awaitableSession.then(session => {
                registerForCompileRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {
                    return {
                        type: SERVICE_ERROR_RESPONSE,
                        requestId: request.type === COMPILE_REQUEST ? request.requestId : undefined,
                        message: "failed to run",
                        statusCode: "503"
                    }
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");

                session.openProject({ package: "console", files: [{ name: "program.cs", content: "" }] });

                session.compile().catch(error => {
                    if (error.message === "failed to run") {
                        done();
                    }
                });
            });
        });
    });
});