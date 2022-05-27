// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as tryDotNetEditor from './tryDotNetEditor';
import * as messages from './legacyTryDotNetMessages';
import * as factory from './factory';
import './index.css';
import * as rxjs from 'rxjs';
import * as monacoAdapterImpl from './monacoAdapterImpl';
import { IServiceError } from './apiService';
import * as dotnetInteractive from '@microsoft/dotnet-interactive';

if (window) {

	const settings: TryDotNetEditorSettings = {
		editorId: "-0-"
	};

	const configuration: factory.IConfiguration = JSON.parse(document.getElementById("trydotnet-editor-script").dataset.trydotnetConfiguration);

	console.log(`[trydotnet-editor] configuration: ${JSON.stringify(configuration)}`);
	if (configuration.enableLogging === true) {
		dotnetInteractive.Logger.configure("trydotnet-editor", (entry) => {
			switch (entry.logLevel) {
				case dotnetInteractive.LogLevel.Info:
					console.log(`[${entry.source}] ${entry.message}`);
					break;
				case dotnetInteractive.LogLevel.Warn:
					console.warn(`[${entry.source}] ${entry.message}`);
					break;
				case dotnetInteractive.LogLevel.Error:
					console.error(`[${entry.source}] ${entry.message}`);
					break;
			}

		});
	}

	const frame = window?.frameElement as HTMLIFrameElement;
	if (frame) {

		let editorId = frame?.dataset["trydotnetEditorId"];
		if (editorId) {
			settings.editorId = editorId.toString();
		}
	}

	let mainWindowOrParent: Window = window;
	if (window.parent) {
		mainWindowOrParent = window.parent;
		dotnetInteractive.Logger.default.info("editor in iframe setup");
	}

	const postAndLog = (message: any) => {

		message.editorId = settings.editorId;
		dotnetInteractive.Logger.default.info(`[sending from trydotnet-editor] ${JSON.stringify(message)}`);
		const messageLogger = window['postMessageLogger'];
		if (messageLogger) {
			messageLogger(message);
		}
		mainWindowOrParent.postMessage(message, '*');
	};

	const mainWindowMessages = new rxjs.Subject<any>();
	window.addEventListener('message', (message) => {
		dotnetInteractive.Logger.default.info(`[received in trydotnet-editor] ${JSON.stringify(message.data)}`);
		const apiMessage = message.data;
		if (apiMessage) {
			mainWindowMessages.next(apiMessage);
		}
	}, false);

	const editor = factory.createEditor(document.body);
	const kernel = factory.createWasmProjectKernel((serviceError: IServiceError) => {
		postAndLog({
			type: messages.SERVICE_ERROR_RESPONSE,
			serviceError: serviceError
		});
	});
	const tdnEditor = new tryDotNetEditor.TryDotNetEditor(message => postAndLog(message), mainWindowMessages, kernel);

	tdnEditor.editor = new monacoAdapterImpl.MonacoEditorAdapter(editor);
	tdnEditor.editorId = settings.editorId;

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
