// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as chai from "chai";
import { autoEnable } from "../src/index";
import { JSDOM } from "jsdom";
import { notifyEditorReady, notifyEditorReadyWithId, registerForRunRequest, registerForRequestIdGeneration, notifyRunReadyWithId } from "./messagingMocks";
import { Done } from "mocha";
import { expect } from "chai";
import { RUN_RESPONSE, RUN_REQUEST, ApiMessage } from "../src/internals/apiMessages";

chai.should();

describe("a user", () => {
    describe("with enriched html", () => {
        it("can create a session with a specific package", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="customPackage" data-trydotnet-session-id="codeSession">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window).then(() => {
                let iframe = dom.window.document.querySelector<HTMLIFrameElement>("body>iframe");
                let src = iframe.getAttribute("src");
                var url = new URL(src);
                url.searchParams.get("workspaceType").should.be.equal("customPackage");
                done();
            });
            notifyEditorReadyWithId(configuration, dom.window, "codeSession::0");
        });

        it("can create a session in debug mode", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="customPackage" data-trydotnet-session-id="codeSession">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") , debug: true}, dom.window.document, dom.window).then(() => {
                let iframe = dom.window.document.querySelector<HTMLIFrameElement>("body>iframe");
                let src = iframe.getAttribute("src");
                var url = new URL(src);
                url.searchParams.get("debug").should.be.equal("true");
                done();
            });
            notifyEditorReadyWithId(configuration, dom.window, "codeSession::0");
        });

        it("can create a session with blazor enabled", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="customPackage" data-trydotnet-session-id="codeSession">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") , useBlazor: true}, dom.window.document, dom.window).then(() => {
                let iframe = dom.window.document.querySelector<HTMLIFrameElement>("body>iframe");
                let src = iframe.getAttribute("src");
                var url = new URL(src);
                url.searchParams.get("useBlazor").should.be.equal("true");
                done();
            });
            notifyEditorReadyWithId(configuration, dom.window, "codeSession::0");
        });

        it("can create a session with blazor disabled", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="customPackage" data-trydotnet-session-id="codeSession">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") , useBlazor: false}, dom.window.document, dom.window).then(() => {
                let iframe = dom.window.document.querySelector<HTMLIFrameElement>("body>iframe");
                let src = iframe.getAttribute("src");
                var url = new URL(src);
                expect( url.searchParams.get("useBlazor")).to.be.null;
                done();
            });
            notifyEditorReadyWithId(configuration, dom.window, "codeSession::0");
        });

        it("fails if a session contains duplicated editor id", () => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="preSession" data-trydotnet-editor-id="editor">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="preSession" data-trydotnet-editor-id="editor">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </coce>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            dom.window.document.querySelectorAll("iframe").length.should.be.equal(0);
            expect(() => autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window))
                .to.throw("editor id preSession::editor already defined");
        });

        it("can create a session with initial project using code element", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="codeSession">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            dom.window.document.querySelectorAll("iframe").length.should.be.equal(0);
            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window).then(() => {
                dom.window.document.querySelectorAll("iframe").length.should.be.equal(1);
                done();
            });
            notifyEditorReady(configuration, dom.window);
        });

        it("can create a session with initial project using code element and a code region", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="codeSession" data-trydotnet-file-name="Program.cs" data-trydotnet-region="function" >
                Console.WriteLine("yes in pre tag");
                </code>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            dom.window.document.querySelectorAll("iframe").length.should.be.equal(0);
            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window).then(() => {
                dom.window.document.querySelectorAll("iframe").length.should.be.equal(1);
                done();
            });
            notifyEditorReady(configuration, dom.window);
        });

        it("can create a session with multiple code elements", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="codeSession" data-trydotnet-editor-id="editorZero" data-trydotnet-file-name="Program.cs">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
                <pre height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="codeSession" data-trydotnet-editor-id="editorOne" data-trydotnet-file-name="SecondFile.cs">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            dom.window.document.querySelectorAll("iframe").length.should.be.equal(0);
            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window).then(() => {
                dom.window.document.querySelectorAll("iframe").length.should.be.equal(2);
                done();
            });
            notifyEditorReadyWithId(configuration, dom.window, "codeSession::editorZero");
            notifyEditorReadyWithId(configuration, dom.window, "codeSession::editorOne");
        });

        it("can create a session and wire run button", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre  height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="a">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
                <button data-trydotnet-mode="run" data-trydotnet-session-id="a">Run</button>
                <div data-trydotnet-mode="runResult" data-trydotnet-session-id="a"></div>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            dom.window.document.querySelectorAll("iframe").length.should.be.equal(0);

            expect(dom.window.document.querySelectorAll("button")[0].onclick).to.be.null;
            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window)
                .then((sessions) => {
                    dom.window.document.querySelectorAll("iframe").length.should.be.equal(1);
                    expect(dom.window.document.querySelectorAll("button")[0].onclick).not.to.be.undefined;
                    sessions.length.should.be.equal(1);
                    sessions[0].editorIframes.length.should.be.equal(1);
                    sessions[0].session.should.not.be.null;
                    sessions[0].runButtons.length.should.be.equal(1);
                    done();
                });
            notifyEditorReadyWithId(configuration, dom.window, "a::0");
        });

        it("can create a session and wire run button and captures args", (done: Done) => {
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre  height="300px" width="800px">
                <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="a">
            using System;
            public class Program {
                public static void Main()
                {
                    Console.WriteLine("yes in pre tag");
                }
            }
                </code>
                </pre>
                <button data-trydotnet-mode="run" data-trydotnet-session-id="a" data-trydotnet-run-args="the stuff dreams are made of &quot;and fame&quot;">Run</button>
                <div data-trydotnet-mode="runResult" data-trydotnet-session-id="a"></div>
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            dom.window.document.querySelectorAll("iframe").length.should.be.equal(0);

            expect(dom.window.document.querySelectorAll("button")[0].onclick).to.be.null;
            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window).then((sessions) => {

                let editorIFrame = sessions[0].editorIframes[0];
                registerForRunRequest(configuration, editorIFrame, dom.window, (request: ApiMessage): ApiMessage => {

                    (<any>request).parameters.runArgs.should.be.equal('the stuff dreams are made of "and fame"');
                    done();
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success"
                    };
                });

                registerForRequestIdGeneration(configuration, editorIFrame, dom.window, (_rid) => "TestRun");
                sessions[0].runButtons[0].click();
            });

            notifyEditorReadyWithId(configuration, dom.window, "a::0");
            notifyRunReadyWithId(configuration, dom.window, "a::0");
        });

        it("can create a session and wire run button and captures args with multiple session", (done: Done) => {

            let argsCaptured: string[] = ["", ""];
            let configuration = {
                hostOrigin: "https://docs.microsoft.com"
            };
            let dom = new JSDOM(
                `<!DOCTYPE html>
            <html lang="en">
            <body>
                <pre  height="300px" width="800px">
                    <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="a">
                    // code a
                    </code>
                </pre>
                <button data-trydotnet-mode="run" data-trydotnet-session-id="a" data-trydotnet-run-args="first session args">Run A</button>
                
                <pre  height="300px" width="800px">
                    <code data-trydotnet-mode="editor" data-trydotnet-package="console" data-trydotnet-session-id="b">
                    // code b
                    </code>
                </pre>
                <button data-trydotnet-mode="run" data-trydotnet-session-id="b" data-trydotnet-run-args="second session args">Run B</button>                
            </body>
            </html>`,
                {
                    url: configuration.hostOrigin,
                    runScripts: "dangerously"
                });

            autoEnable({ apiBaseAddress: new URL("https://try.dot.net") }, dom.window.document, dom.window).then((sessions) => {

                sessions.length.should.be.equal(2);
                let editorIFrameA = sessions[0].editorIframes[0];
                let editorIFrameB = sessions[1].editorIframes[0];

                registerForRunRequest(configuration, editorIFrameA, dom.window, (request: ApiMessage): ApiMessage => {

                    argsCaptured[0] = (<any>request).parameters.runArgs;
                    if ((argsCaptured[0] === "first session args") && (argsCaptured[1] === "second session args")) {
                        done();
                    }
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success"
                    };
                });

                registerForRunRequest(configuration, editorIFrameB, dom.window, (request: ApiMessage): ApiMessage => {
                    argsCaptured[1] = (<any>request).parameters.runArgs;
                    if ((argsCaptured[0] === "first session args") && (argsCaptured[1] === "second session args")) {
                        done();
                    }
                    return {
                        type: RUN_RESPONSE,
                        requestId: request.type === RUN_REQUEST ? request.requestId : undefined,
                        outcome: "Success"
                    };
                });

                registerForRequestIdGeneration(configuration, editorIFrameA, dom.window, (_rid) => "TestRun");
                registerForRequestIdGeneration(configuration, editorIFrameB, dom.window, (_rid) => "TestRun");

                sessions[0].runButtons[0].click();
                sessions[1].runButtons[0].click();
            });


            notifyEditorReadyWithId(configuration, dom.window, "a::0");
            notifyEditorReadyWithId(configuration, dom.window, "b::0");

            notifyRunReadyWithId(configuration, dom.window, "a::0");
            notifyRunReadyWithId(configuration, dom.window, "b::0");

        });
    });
});
