// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import acceptCompletionItemSpecs from "./mlsClient.acceptCompletionItem.specs";
import run from "./mlsClient.run.specs";
import getCompletionListSpecs from "./mlsClient.getCompletionList.specs";
import getSignatureHelpSpecs from "./mlsClient.getSignatureHelp.specs";
import getSourceCodeSpecs from "./mlsClient.getSourceCode.specs";
import getWorkspaceFromGist from "./mlsClient.getWorkspaceFromGist.specs";
import getProjectFromGist from "./mlsClient.getProjectFromGist.specs";
import getRegionFromFiles from "./mlsClient.getRegionFromFiles.specs";
import ICanGetAClient from "./ICanGetAClient";

export default function mlsClientSpecs(getClient: ICanGetAClient, scenario: string) {
    describe(scenario, () => {
        acceptCompletionItemSpecs(getClient);
        run(getClient);
        getCompletionListSpecs(getClient);
        getSourceCodeSpecs(getClient);
        getSignatureHelpSpecs(getClient);
        getWorkspaceFromGist(getClient);
        getProjectFromGist(getClient);
        getRegionFromFiles(getClient);
    });
} 
