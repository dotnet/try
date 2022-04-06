// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as tryDotNetEditor from './tryDotNetEditor';
import * as messages from './messages';
import * as factory from './factory';
import './index.css';
import * as rxjs from 'rxjs';
import * as monacoAdapterImpl from './monacoAdapterImpl';

if (window) {

	const postAndLog = (message: any) => {
		//console.log(`[from Editor] ${JSON.stringify(message)}`);
		const messageLogger = window['postMessageLogger'];
		if (messageLogger) {
			messageLogger(message);
		}

		window.postMessage(message, '*');
	};

	const mainWindowMessages = new rxjs.Subject<any>();
	window.addEventListener('message', (message) => {
		//console.log(`[received in editor] ${JSON.stringify(message.data)}`);
		const apiMessage = <messages.AnyApiMessage>message.data;
		if (apiMessage) {
			mainWindowMessages.next(apiMessage);
		}
	}, false);

	const editor = factory.createEditor(document.body);
	const kernel = factory.createWasmProjectKernel();
	const tdnEditor = new tryDotNetEditor.TryDotNetEditor(message => postAndLog(message), mainWindowMessages, kernel);

	tdnEditor.editor = new monacoAdapterImpl.MonacoEditorAdapter(editor);

	window['trydotnetEditor'] = tdnEditor;

	// for messaging api backward compatibility
	postAndLog({
		type: "NOTIFY_HOST_EDITOR_READY"
	});
}
