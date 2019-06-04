// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Project, SourceFile } from "../project";
import { createSessionWithProjectAndOpenDocuments } from "../sessionFactory";
import { RunResult, ServiceError, RunConfiguration } from "../session";
import { isNullOrUndefinedOrWhitespace } from "../stringExtensions";
import { DocumentsToOpen } from "../internals/session";
import { IOutputPanel } from "../outputPanel";
import { XtermTerminal } from "../XtermTerminal";
import {
    tryDotNetOutputModes,
    TryDotNetSession,
    tryDotNetModes,
    AutoEnablerConfiguration,
    SessionLookup
} from "./types";
import { getTrydotnetSessionId, getTrydotnetEditorId } from "./utilities";
import { getCodeContainer, getCode } from "./codeProcessor";
import { extractIncludes, mergeFiles, getDocumentsToInclude } from "./includesProcessor";
import { Configuration } from "../configuration";
import { PreOutputPanel } from "../preOutputPanel";

function cloneSize(source: HTMLElement, destination: HTMLElement) {
    let width = source.getAttribute("width");
    if (width) {
        destination.setAttribute("width", width);
    }

    let height = source.getAttribute("height");
    if (height) {
        destination.setAttribute("height", height);
    }

    let style = source.getAttribute("style");
    if (style) {
        destination.setAttribute("style", style);
    }
}

function clearRunClasses(element: HTMLElement) {
    if (element) {
        element.classList.remove("run-execution");
        element.classList.remove("run-failure");
        element.classList.remove("run-success");
        element.classList.remove("busy");
    }
}
const defaultRunResultHandler = (
    runResult: RunResult,
    container: HTMLElement,
    _sessionId: string
) => {
    if (container) {
        clearRunClasses(container);
        container.classList.remove("collapsed");
        container.classList.add(
            runResult.succeeded ? "run-success" : "run-failure"
        );
    }
};

const defaultServiceErrorHandler = (
    error: ServiceError,
    container: HTMLElement,
    serviceErrorPanel: IOutputPanel,
    _sessionId: string
) => {
    if (container) {
        clearRunClasses(container);
        container.classList.add("run-failure");
    }
    if (serviceErrorPanel) {
        serviceErrorPanel.setContent(error.message);
    }
};

export function autoEnable(
    { apiBaseAddress, useBlazor = true, debug = false, runResultHandler = defaultRunResultHandler, serviceErrorHandler = defaultServiceErrorHandler }: AutoEnablerConfiguration,
    documentToScan?: HTMLDocument,
    mainWindow?: Window
): Promise<TryDotNetSession[]> {
    return internalAutoEnable(
        {
            apiBaseAddress: apiBaseAddress,
            useBlazor: useBlazor,
            debug: debug,
            runResultHandler: runResultHandler,
            serviceErrorHandler: serviceErrorHandler
        },
        documentToScan,
        mainWindow
    );
}

export function tryParseEnum(outputType?: string): tryDotNetOutputModes {
    if (isNullOrUndefinedOrWhitespace(outputType)) {
        return tryDotNetOutputModes.standard;
    }

    for (let n in tryDotNetOutputModes) {
        const name = tryDotNetOutputModes[n];
        console.log(name);
        if (name === outputType) {
            return <tryDotNetOutputModes>n;
        }
    }
}

export function createOutputPanel(
    outputDiv: HTMLDivElement,
    outputType?: string
): IOutputPanel {
    let type: tryDotNetOutputModes = tryParseEnum(outputType);
    let outputPanel: IOutputPanel;

    switch (type) {
        case tryDotNetOutputModes.terminal:
            outputPanel = new XtermTerminal(outputDiv);
            break;
        default:
            outputPanel = new PreOutputPanel(outputDiv);
            break;
    }

    return outputPanel;
}

export function createRunOutputElements(
    outputPanelContainer: HTMLDivElement,
    doc: HTMLDocument
): { outputPanel: IOutputPanel } {
    if (outputPanelContainer === null || outputPanelContainer === undefined) {
        return {
            outputPanel: null
        };
    }
    let outputDiv = outputPanelContainer.appendChild(doc.createElement("div"));
    outputDiv.classList.add("trydotnet-output");
    let outputType = outputPanelContainer.dataset.trydotnetOutputType;
    let outputPanel: IOutputPanel = createOutputPanel(outputDiv, outputType);

    return {
        outputPanel
    };
}

function internalAutoEnable(
    configuration: AutoEnablerConfiguration,
    documentToScan?: HTMLDocument,
    mainWindow?: Window
): Promise<TryDotNetSession[]> {

    let doc = documentToScan ? documentToScan : document;
    let wnd = mainWindow ? mainWindow : window;
    let apiBaseAddress = configuration.apiBaseAddress;
    let sessions: SessionLookup = {};

    let codeSources = doc.querySelectorAll(
        `pre>code[data-trydotnet-mode=${tryDotNetModes[tryDotNetModes.editor]}]`
    );

    let runButtons = doc.querySelectorAll(
        `button[data-trydotnet-mode=${tryDotNetModes[tryDotNetModes.run]}]`
    );

    let outputDivs = doc.querySelectorAll(
        `div[data-trydotnet-mode=${tryDotNetModes[tryDotNetModes.runResult]}]`
    );

    let errorDivs = doc.querySelectorAll(
        `div[data-trydotnet-mode=${tryDotNetModes[tryDotNetModes.errorReport]}]`
    );

    codeSources.forEach(async (codeSource: HTMLElement) => {
        let sessionId = getTrydotnetSessionId(codeSource);
        if (!sessions[sessionId]) {
            sessions[sessionId] = { codeSources: [] };
        }
        sessions[sessionId].codeSources.push(codeSource);
    });

    runButtons.forEach(async (run: HTMLButtonElement) => {
        let sessionId = getTrydotnetSessionId(run);
        if (sessions[sessionId]) {
            sessions[sessionId].runButton = run;
        }
    });

    outputDivs.forEach(async (output: HTMLDivElement) => {
        let sessionId = getTrydotnetSessionId(output);
        if (sessions[sessionId]) {
            sessions[sessionId].outputPanel = output;
        }
    });

    errorDivs.forEach(async (error: HTMLDivElement) => {
        let sessionId = getTrydotnetSessionId(error);
        if (sessions[sessionId]) {
            sessions[sessionId].errorPanel = error;
        }
    });

    let editorIds = new Set();
    let filedNameSeed = 0;
    let awaitableSessions: Promise<TryDotNetSession>[] = [];

    let includes = extractIncludes(doc);

    for (let sessionId in sessions) {
        let session = sessions[sessionId];
        let iframes: HTMLIFrameElement[] = [];
        let files: SourceFile[] = [];
        let packageName: string = null;
        let packageVersion: string = null;
        let language: string = "csharp";
        let documentsToOpen: DocumentsToOpen = {};
        let editorCount = -1;

        for (let codeSource of session.codeSources) {
            editorCount++;

            let codeContainer = getCodeContainer(codeSource);
            let code = getCode(codeSource);

            let pacakgeAttribute = codeSource.dataset.trydotnetPackage;
            if (!isNullOrUndefinedOrWhitespace(pacakgeAttribute)) {
                packageName = pacakgeAttribute;
            }

            let packageVersionAttribute = codeSource.dataset.dataTrydotnetPackageVersion;
            if (!isNullOrUndefinedOrWhitespace(packageVersionAttribute)) {

                packageVersion = packageVersionAttribute;
            }

            
            let languageAttribute = codeSource.dataset.trydotnetLanguage;
            if (!isNullOrUndefinedOrWhitespace(languageAttribute)) {
                language = languageAttribute;
            }

            let editorId = getTrydotnetEditorId(codeSource);

            if (!packageName) {
                throw new Error("must provide package");
            }

            let filename = codeSource.dataset.trydotnetFileName;
            if (!filename) {
                filename = `code_file_${filedNameSeed}.cs`;
                filedNameSeed++;
            }

            let iframe: HTMLIFrameElement = doc.createElement("iframe");

            if (isNullOrUndefinedOrWhitespace(editorId)) {
                editorId = editorCount.toString();
            }

            editorId = `${sessionId}::${editorId}`;

            if (editorIds.has(editorId)) {
                throw new Error(`editor id ${editorId} already defined`);
            }

            editorIds.add(editorId);

            // progapate attributes to iframe
            iframe.dataset.trydotnetEditorId = editorId;
            iframe.dataset.trydotnetSessionId = sessionId;

            let region = codeSource.dataset.trydotnetRegion;
            if (!isNullOrUndefinedOrWhitespace(region)) {
                documentsToOpen[editorId] = {
                    fileName: filename,
                    region: region,
                    content: code
                };
            } else {
                files.push({ name: filename, content: code });
                documentsToOpen[editorId] = filename;
            }

            cloneSize(codeContainer, iframe);
            codeContainer.parentNode.replaceChild(iframe, codeContainer);

            iframes.push(iframe);
        }

        let prj: Project = {
            package: packageName,            
            files: mergeFiles(files, includes, sessionId)
        };

        if (!isNullOrUndefinedOrWhitespace(packageVersion)) {
            prj.packageVersion = packageVersion;
        }

        if (!isNullOrUndefinedOrWhitespace(language)) {
            prj.language = language;
        }

        let documentsToInclude = getDocumentsToInclude(includes, sessionId);
        let config: Configuration = {
            debug: configuration.debug,
            useBlazor: configuration.useBlazor,
            hostOrigin: doc.location.origin,
            trydotnetOrigin: apiBaseAddress ? apiBaseAddress.href : null,
            editorConfiguration: {
                options: {
                    minimap: {
                        enabled: false
                    }
                }
            }
        };

        let awaitable = createSessionWithProjectAndOpenDocuments(
            config,
            iframes,
            wnd,
            prj,
            documentsToOpen,
            documentsToInclude
        ).then(tdnSession => {
            let outputPanelContainer = session.outputPanel;
            let errorPanelContainer = session.errorPanel;

            let errorPanel: IOutputPanel;

            let { runResultHandler, serviceErrorHandler } = configuration;

            let { outputPanel } = createRunOutputElements(
                outputPanelContainer,
                doc
            );

            if (
                errorPanelContainer === null ||
                errorPanelContainer === undefined
            ) {
                // fall back on output modules
                errorPanelContainer = outputPanelContainer;
                errorPanel = outputPanel;
            } else {
                // todo: impelement createErrorReportElements
                throw new Error("Not implemented");
            }

            let createdSession: TryDotNetSession = {
                sessionId: sessionId,
                session: tdnSession,
                editorIframes: iframes
            };
            if (outputPanel) {
                outputPanel.initialise();
                createdSession.outputPanels = [outputPanel];
                tdnSession.subscribeToOutputEvents(event => {
                    if (event.stdout) {
                        outputPanel.append(event.stdout);
                    }
                    if (event.exception) {
                        outputPanel.append(
                            `Unhandled Exception: ${event.exception}`
                        );
                    }
                });

                tdnSession.subscribeToServiceErrorEvents(error => {
                    errorPanel.append(error.message);
                });
            }

            let runButton = session.runButton;

            if (runButton) {
                tdnSession.onCanRunChanged(
                    canRun => {
                        runButton.disabled = !canRun;
                    }
                )

                createdSession.runButtons = [runButton];

                runButton.onclick = () => {
                    clearRunClasses(runButton);
                    runButton.classList.add("busy");
                    if (outputPanelContainer) {
                        clearRunClasses(outputPanelContainer);
                        outputPanelContainer.classList.add("run-execution");
                    }
                    if (outputPanel) {
                        outputPanel.clear();
                    }



                    let runOptions: RunConfiguration = {};

                    let args = runButton.dataset.trydotnetRunArgs;

                    if (!isNullOrUndefinedOrWhitespace(args)) {
                        runOptions.runArgs = args;
                    }
                    return tdnSession
                        .run(runOptions)
                        .then(runResult => {
                            clearRunClasses(runButton);
                            if (runResultHandler) {
                                runResultHandler(
                                    runResult,
                                    outputPanelContainer,
                                    sessionId
                                );
                            }
                        })
                        .catch(error => {
                            runButton.disabled = false;
                            clearRunClasses(runButton);
                            if (serviceErrorHandler) {
                                serviceErrorHandler(
                                    error,
                                    errorPanelContainer,
                                    errorPanel,
                                    sessionId
                                );
                            }
                        });
                };
            }

            return createdSession;
        });

        awaitableSessions.push(awaitable);
    }

    return Promise.all(awaitableSessions);
}
