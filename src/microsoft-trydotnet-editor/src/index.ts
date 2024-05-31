// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as tryDotNetEditor from './tryDotNetEditor';
import * as messages from './legacyTryDotNetMessages';
import * as factory from './factory';
import './index.css';
import * as rxjs from 'rxjs';
import * as monacoAdapterImpl from './monacoAdapterImpl';
import * as apiService from './apiService';
import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';
import { configureLogging } from './log';

if (window) {

	const settings: TryDotNetEditorSettings = {
		editorId: "-0-"
	};

	const configuration: factory.IConfiguration = JSON.parse(document.getElementById("trydotnet-editor-script").dataset.trydotnetConfiguration);

	console.log(`[trydotnet-editor] configuration: ${JSON.stringify(configuration)}`);

	configureLogging({ enableLogging: configuration.enableLogging });

	const frame = window?.frameElement as HTMLIFrameElement;
	if (frame) {

		let editorId = frame?.dataset["trydotnetEditorId"];
		if (editorId) {
			settings.editorId = editorId.toString();
		}
	}

	let messageDestination = "";
	let mainWindowOrParent: Window = window;
	if (window.parent) {
		mainWindowOrParent = window.parent;
		polyglotNotebooks.Logger.default.info("editor in iframe setup");
		messageDestination = `" to hosting window ${document.referrer}`;
	}

	const postAndLog = (message: any) => {

		message.editorId = settings.editorId;
		polyglotNotebooks.Logger.default.info(`[sending from trydotnet-editor${messageDestination}] ${JSON.stringify(message)}`);
		const messageLogger = window['postMessageLogger'];
		if (messageLogger) {
			messageLogger(message);
		}
		mainWindowOrParent.postMessage(message, '*');
	};

	const mainWindowMessages = new rxjs.Subject<any>();
	window.addEventListener('message', (message) => {
		polyglotNotebooks.Logger.default.info(`[received in trydotnet-editor] ${JSON.stringify(message)}`);
		const apiMessage = message.data;
		if (apiMessage) {
			mainWindowMessages.next(apiMessage);
		}
	}, false);

	const container = configuration.editorContainer ? document.getElementById(configuration.editorContainer) : document.body;
	const editor = factory.createEditor(container ?? document.body);
	const kernel = factory.createWasmProjectKernel((serviceError: apiService.IServiceError) => {
		postAndLog({
			type: messages.SERVICE_ERROR_RESPONSE,
			serviceError: serviceError
		});
	});
	const tdnEditor = new tryDotNetEditor.TryDotNetEditor(message => postAndLog(message), mainWindowMessages, kernel);

	tdnEditor.editor = new monacoAdapterImpl.MonacoEditorAdapter(editor);
	tdnEditor.editorId = settings.editorId;
    document.body.classList.add('monaco-editor-background');

	window['trydotnetEditor'] = tdnEditor;

	// for messaging api backward compatibility
	postAndLog({
		type: messages.HOST_EDITOR_READY_EVENT
	});
	postAndLog({
		type: messages.HOST_RUN_READY_EVENT
	});
}

interface TryDotNetEditorSettings {
	editorId: string;
};
