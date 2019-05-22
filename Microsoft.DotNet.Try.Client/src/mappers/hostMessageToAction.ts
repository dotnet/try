// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import actions from "../actionCreators/actions";
import { CREATE_REGIONS_FROM_SOURCEFILES_REQUEST, CREATE_PROJECT_FROM_GIST_REQUEST, CREATE_OPERATION_ID_REQUEST, RUN_REQUEST, SHOW_EDITOR_REQUEST, FOCUS_EDITOR_REQUEST, CONFIGURE_MONACO_REQUEST, DEFINE_THEMES_REQUEST, SET_WORKSPACE_REQUEST, ENABLE_INSTRUMENTATION, CHECK_CAN_MOVE_TO_PREVIOUS_INSTRUMENTATION, CHECK_CAN_MOVE_TO_NEXT_INSTRUMENTATION, MOVE_TO_PREVIOUS_INSTRUMENTATION, MOVE_TO_NEXT_INSTRUMENTATION, SET_EDITOR_CODE_REQUEST, SET_ACTIVE_BUFFER_REQUEST } from "../constants/ApiMessageTypes";

const mappers: any = {
    [CONFIGURE_MONACO_REQUEST]: (m: any) => actions.configureMonacoEditor(m.editorOptions, m.theme),
    [DEFINE_THEMES_REQUEST]: (m: any) => actions.defineMonacoEditorThemes(m.themes),
    [FOCUS_EDITOR_REQUEST]: () => actions.focusMonacoEditor(),
    [RUN_REQUEST]: (m: any) => actions.run(m.requestId, m.parameters),
    [SHOW_EDITOR_REQUEST]: () => actions.showEditor(),
    setRunResult: (m: any) => actions.runCodeResultSpecified(m.output, m.succeeded),
    [SET_EDITOR_CODE_REQUEST]: (m: any) => actions.updateCurrentWorkspaceBuffer(m.sourceCode),
    [SET_WORKSPACE_REQUEST]: (m: any) => actions.setWorkspaceAndActiveBuffer(m.workspace, m.bufferId),
    setWorkspaceFromGist: (m: any) => actions.LoadWorkspaceFromGist(m.gistId, m.bufferId, m.workspaceType, m.canShowGitHubPanel),
    setAdditionalUsings: (m: any) => actions.setAdditionalUsings(m.additionalUsings),
    [ENABLE_INSTRUMENTATION]: (m: any) => actions.setInstrumentation(m.enabled),
    [CHECK_CAN_MOVE_TO_PREVIOUS_INSTRUMENTATION]: () => actions.checkPrevEnabled(),
    [CHECK_CAN_MOVE_TO_NEXT_INSTRUMENTATION]: () => actions.checkNextEnabled(),
    [MOVE_TO_NEXT_INSTRUMENTATION]: () => actions.nextInstrumentationStep(),
    [MOVE_TO_PREVIOUS_INSTRUMENTATION]: () => actions.prevInstrumentationStep(),
    applyScaffolding: (m: any) => actions.applyScaffolding(m.scaffoldingType, m.fileName, m.additionalUsings),
    [CREATE_OPERATION_ID_REQUEST]: (m: any) => actions.generateOperationId(m.requestId),
    [CREATE_PROJECT_FROM_GIST_REQUEST]: (m: any) => actions.createProjectFromGist(m.requestId, m.projectTemplate, m.gistId, m.commitHash),
    [CREATE_REGIONS_FROM_SOURCEFILES_REQUEST]: (m: any) => actions.createRegionsFromProjectFiles(m.requestId, m.files),
    [SET_ACTIVE_BUFFER_REQUEST]: (m: any) => actions.setActiveBuffer(m.bufferId)

};

export default (hostMessage: any) => {
    if (!hostMessage || !hostMessage.type) {
        return null;
    }

    var mapper: any = mappers[hostMessage.type];

    if (mapper) {
        return mapper(hostMessage);
    }
};
