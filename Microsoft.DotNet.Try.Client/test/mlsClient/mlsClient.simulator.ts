// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IRunRequest, IGistWorkspace } from "../../src/IMlsClient";
import MlsClient, { ICanFetch, Request, Response } from "../../src/MlsClient";
import { CookieGetter } from "../../src/constants/CookieGetter";
import { IWorkspace } from "../../src/IState";
import { baseAddress as defaultBaseAddress } from "./constantUris";
import { clientConfigurationExample, defaultGistProject } from "../testResources";
import { NullAIClient } from "../../src/ApplicationInsights";
import { CreateProjectFromGistRequest, CreateProjectResponse, CreateRegionsFromFilesRequest, CreateRegionsFromFilesResponse } from "../../src/clientApiProtocol";

export default class MlsClientSimulator extends MlsClient {
    constructor(baseAddress: URL = defaultBaseAddress) {
        super(
            getSimulatingFetcher(baseAddress),
            baseAddress,
            getSimulatedCookieGetter(),
            new NullAIClient(),
            baseAddress);
    }
}

const nextTick = async () =>
    new Promise((resolve) => {
        setTimeout(resolve, 1);
    });

async function serveScriptWorkspaceTypeRequests(workspace: IWorkspace): Promise<Response> {
    const code = workspace.buffers[0].content;

    if (code === "Console.WriteLine(\"Hello, World\");") {
        return Promise.resolve({
            json: async () => Promise.resolve({
                output: ["Hello, World"],
                succeeded: true,
                variables: [{
                    name: "output",
                    states: [{
                        lineNumber: 1,
                        value: "Hello, World"
                    }],
                    value: "Hello, World"
                }]
            }),
            text: async () => Promise.resolve(""),
            ok: true
        });
    }

    if (code === "var output = \"Hello, World\"; Console.WriteLine(output);") {
        return Promise.resolve({
            json: async () => Promise.resolve({
                output: ["Hello, World"],
                succeeded: true
            }),
            text: async () => Promise.resolve(""),
            ok: true
        });
    }

    if (code === "throw new Exception(\"Goodbye, World\");") {
        return Promise.resolve({
            json: async () => Promise.resolve({
                exception: "System.Exception: Goodbye, World",
                output: ["Hello, World"],
                succeeded: true
            }),
            text: async () => Promise.resolve(""),
            ok: true
        });
    }

    if (code === "Console.WriteLine(\"Hello, World\");throw new Exception(\"Goodbye, World\");") {
        return Promise.resolve({
            json: async () => Promise.resolve({
                output: ["Hello, World"],
                succeeded: true
            }),
            text: async () => Promise.resolve(""),
            ok: true
        });
    }

    if (code === "Console.PrintLine();") {
        return Promise.resolve({
            json: async () => Promise.resolve({
                diagnostics: [{
                    start: 8,
                    end: 17,
                    message: "'Console' does not contain a definition for 'PrintLine'",
                    severity: 3,
                    id: "CS0117"
                }],
                output: [],
                succeeded: false
            }),
            text: async () => Promise.resolve(""),
            ok: true
        });
    }
}

const getSimulatingFetcher = (baseAddress: URL): ICanFetch => {
    return async (uri: string, request: Request) => {
        await nextTick();

        let relativeUri = uri.replace(baseAddress.href, "/");

        if (relativeUri.startsWith("/clientConfiguration")) {
            return Promise.resolve({
                json: async () => Promise.resolve(JSON.parse(JSON.stringify(clientConfigurationExample))),
                ok: true
            });
        }

        if (relativeUri.startsWith("/workspace/acceptCompletionItem")) {
            if (relativeUri.indexOf("listId=7fc72521-7293-4781-affa-041f787cfe8e") > 0
                && relativeUri.indexOf("index=0") > 0) {
                return Promise.resolve({
                    json: async () => Promise.resolve({}),
                    ok: true
                });
            }
        }

        if (relativeUri.startsWith("/workspace/run")) {
            let runRequest = JSON.parse(request.body) as IRunRequest;
            let workspace = runRequest.workspace;
            switch (workspace.workspaceType) {
                case "script":
                    return serveScriptWorkspaceTypeRequests(workspace);
            }

            throw new Error(`workspace :${workspace}`);
        }

        if (relativeUri.startsWith("/workspace/fromgist/")) {
            const url = require("url");
            const queryData = url.parse(relativeUri, true).query;
            let workspaceInfo: IGistWorkspace = {
                originType: "gist",
                htmlUrl: "https://gist.github.com/df44833326fcc575e8169fccb9d41fc7",
                rawFileUrls: [
                    { fileName: "Program.cs", url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/35765c05ddb54bc827419211a6b645473cdda7f9/FibonacciGenerator.cs" },
                    { fileName: "FibonacciGenerator.cs", url: "https://gist.githubusercontent.com/colombod/df44833326fcc575e8169fccb9d41fc7/raw/700a834733fa650d2a663ccd829f8a9d09b44642/Program.cs" }
                ],
                workspace: {
                    workspaceType: queryData.workspaceType,
                    buffers: [
                        { id: "Program.cs", content: "console code", position: 0 },
                        { id: "FibonacciGenerator.cs", content: "generator code", position: 0 }]
                }
            };

            return Promise.resolve({
                json: async () => Promise.resolve(workspaceInfo),
                ok: true
            });
        }

        if (relativeUri.startsWith("/project/fromGist")) {
            let createRequest = JSON.parse(request.body) as CreateProjectFromGistRequest;
            let response: CreateProjectResponse = {
                project: { ...defaultGistProject },
                requestId: createRequest.requestId
            };
            response.project.projectTemplate = createRequest.projectTemplate;
            return Promise.resolve({
                json: async () => Promise.resolve(response),
                ok: true
            });
        }

        if (relativeUri.startsWith("/project/files/regions")) {
            let createRequest = JSON.parse(request.body) as CreateRegionsFromFilesRequest;
            let response: CreateRegionsFromFilesResponse = {
                regions: createRequest.files.map(file => {
                    let id = file.name;
                    let content = file.content;
                    let parts = content.split("$$");
                    if (parts.length > 1) {
                        id = `${id}@${parts.shift()}`;
                        content = parts.join(" ");
                    }
                    return { id: id, content: content };
                }),
                requestId: createRequest.requestId
            };
            return Promise.resolve({
                json: async () => Promise.resolve(response),
                ok: true
            });
        }

        if (relativeUri.startsWith("/snippet")) {
            if (relativeUri.indexOf(`404`) >= 0) {
                return Promise.resolve({
                    json: async () => Promise.resolve({}),
                    ok: false
                });
            }

            if (relativeUri.indexOf(`some%2Fsource%2Ffile.cs`) >= 0) {
                return Promise.resolve({
                    json: async () => Promise.resolve({
                        buffer: "Console.WriteLine(\"Hello, World\");",
                    }),
                    ok: true
                });
            }

            throw new Error(`Unrecognized snippet request with url: ${uri}`);
        }

        if (relativeUri.startsWith("/workspace/completion")) {
            if (relativeUri.indexOf("completionProvider=pythia") >= 0) {
                return Promise.resolve({
                    json: async () => Promise.resolve({
                        items: [
                            {
                                acceptanceUri: "/workspace/acceptCompletionItem?listId=e08b2e41-3818-4ca2-a251-31bafebe98c0&index=0",
                                displayText: "★ WriteLine",
                                documentation: {
                                    value: "Writes the current line terminator to the standard output stream.\nSystem.IO.IOException: An I/O error occurred.",
                                    isTrusted: false
                                },
                                filterText: "WriteLine",
                                insertText: "WriteLine",
                                kind: "Method",
                                sortText: "0"
                            },
                            {
                                acceptanceUri: "/workspace/acceptCompletionItem?listId=e08b2e41-3818-4ca2-a251-31bafebe98c0&index=1",
                                displayText: "BackgroundColor",
                                documentation: {
                                    value: "Gets or sets the background color of the console.\nReturns: A value that specifies the background color of the console; that is, the color that appears behind each character. The default is black.\nSystem.ArgumentException: The color specified in a set operation is not a valid member of System.ConsoleColor .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
                                    isTrusted: false
                                },
                                filterText: "BackgroundColor",
                                insertText: "BackgroundColor",
                                kind: "Property",
                                sortText: "BackgroundColor"
                            },
                            {
                                acceptanceUri: "/workspace/acceptCompletionItem?listId=e08b2e41-3818-4ca2-a251-31bafebe98c0&index=2",
                                displayText: "Beep",
                                documentation: {
                                    value: "Plays the sound of a beep through the console speaker.\nSystem.Security.HostProtectionException: This method was executed on a server, such as SQL Server, that does not permit access to a user interface.",
                                    isTrusted: false
                                },
                                filterText: "Beep",
                                insertText: "Beep",
                                kind: "Method",
                                sortText: "Beep"
                            },
                            {
                                acceptanceUri: "/workspace/acceptCompletionItem?listId=e08b2e41-3818-4ca2-a251-31bafebe98c0&index=3",
                                displayText: "BufferHeight",
                                documentation: {
                                    value: "Gets or sets the height of the buffer area.\nReturns: The current height, in rows, of the buffer area.\nSystem.ArgumentOutOfRangeException: The value in a set operation is less than or equal to zero.   -or-   The value in a set operation is greater than or equal to System.Int16.MaxValue .   -or-   The value in a set operation is less than System.Console.WindowTop + System.Console.WindowHeight .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
                                    isTrusted: false
                                },
                                filterText: "BufferHeight",
                                insertText: "BufferHeight",
                                kind: "Property",
                                sortText: "BufferHeight"
                            },
                            {
                                acceptanceUri: "/workspace/acceptCompletionItem?listId=e08b2e41-3818-4ca2-a251-31bafebe98c0&index=4",
                                displayText: "BufferWidth",
                                documentation: {
                                    value: "Gets or sets the width of the buffer area.\nReturns: The current width, in columns, of the buffer area.\nSystem.ArgumentOutOfRangeException: The value in a set operation is less than or equal to zero.   -or-   The value in a set operation is greater than or equal to System.Int16.MaxValue .   -or-   The value in a set operation is less than System.Console.WindowLeft + System.Console.WindowWidth .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
                                    isTrusted: false
                                },
                                filterText: "BufferWidth",
                                insertText: "BufferWidth",
                                kind: "Property",
                                sortText: "BufferWidth"
                            }]
                    }),
                    ok: true
                });
            }  

            return Promise.resolve({
                json: async () => Promise.resolve({
                    items: [{
                        acceptanceUri: null,
                        displayText: "BackgroundColor",
                        documentation: {
                            value: "Gets or sets the background color of the console.\nReturns: A value that specifies the background color of the console; that is, the color that appears behind each character. The default is black.\nSystem.ArgumentException: The color specified in a set operation is not a valid member of System.ConsoleColor .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
                            isTrusted: false
                        },
                        filterText: "BackgroundColor",
                        insertText: "BackgroundColor",
                        kind: "Property",
                        sortText: "BackgroundColor"
                    },
                    {
                        acceptanceUri: null,
                        displayText: "Beep",
                        documentation: {
                            value: "Plays the sound of a beep through the console speaker.\nSystem.Security.HostProtectionException: This method was executed on a server, such as SQL Server, that does not permit access to a user interface.",
                            isTrusted: false
                        },
                        filterText: "Beep",
                        insertText: "Beep",
                        kind: "Method",
                        sortText: "Beep"
                    },
                    {
                        acceptanceUri: null,
                        displayText: "BufferHeight",
                        documentation: {
                            value: "Gets or sets the height of the buffer area.\nReturns: The current height, in rows, of the buffer area.\nSystem.ArgumentOutOfRangeException: The value in a set operation is less than or equal to zero.   -or-   The value in a set operation is greater than or equal to System.Int16.MaxValue .   -or-   The value in a set operation is less than System.Console.WindowTop + System.Console.WindowHeight .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
                            isTrusted: false
                        },
                        filterText: "BufferHeight",
                        insertText: "BufferHeight",
                        kind: "Property",
                        sortText: "BufferHeight"
                    },
                    {
                        acceptanceUri: null,
                        displayText: "BufferWidth",
                        documentation: {
                            value: "Gets or sets the width of the buffer area.\nReturns: The current width, in columns, of the buffer area.\nSystem.ArgumentOutOfRangeException: The value in a set operation is less than or equal to zero.   -or-   The value in a set operation is greater than or equal to System.Int16.MaxValue .   -or-   The value in a set operation is less than System.Console.WindowLeft + System.Console.WindowWidth .\nSystem.Security.SecurityException: The user does not have permission to perform this action.\nSystem.IO.IOException: An I/O error occurred.",
                            isTrusted: false
                        },
                        filterText: "BufferWidth",
                        insertText: "BufferWidth",
                        kind: "Property",
                        sortText: "BufferWidth"
                    },
                    {
                        acceptanceUri: null,
                        displayText: "CancelKeyPress",
                        documentation: {
                            value: "Occurs when the System.ConsoleModifiers.Control  modifier key (Ctrl) and either the System.ConsoleKey.C  console key (C) or the Break key are pressed simultaneously (Ctrl+C or Ctrl+Break).",
                            isTrusted: false
                        },
                        filterText: "CancelKeyPress",
                        insertText: "CancelKeyPress",
                        kind: "Event",
                        sortText: "CancelKeyPress"
                    }
                ],
                    diagnostics: [{
                        start: 201,
                        end: 201,
                        message: "Program.cs(11,29): error CS1002: ; expected",
                        severity: 3,
                        id: "CS1002"
                    }]
                }),
                ok: true
            });
        }

        if (relativeUri.startsWith("/workspace/signatureHelp")) {
            return Promise.resolve({
                json: async () => Promise.resolve({
                    activeParameter: 0,
                    activeSignature: 0,
                    signatures: [
                        {
                            name: "Write",
                            label: "void Console.Write(bool value)",
                            documentation: {
                                value: "Writes the text representation of the specified Boolean value to the standard output stream.",
                                  isTrusted: false
                            },
                            parameters: [
                            {
                                name: "value",
                                label: "bool value",
                                documentation: {
                                    value: "**value**: The value to write.",
                                    isTrusted: false
                                }
                            }]
                        },
                    ],
                    diagnostics: [{
                        start: 201,
                        end: 201,
                        message: "Program.cs(11,29): error CS1002: ; expected",
                        severity: 3,
                        id: "CS1002"
                    }]
                }),
                ok: true
            });
        }

        throw new Error(`Unrecognized fetch with uri: ${uri}\nrelativeUri:${relativeUri}\n and request: ${JSON.stringify(request)}`);
    };
};

function getSimulatedCookieGetter(): CookieGetter {
    return () => undefined;
}
