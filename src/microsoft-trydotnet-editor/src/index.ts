// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as tryDotNetEditor from './tryDotNetEditor';
import * as messages from './messages';
import * as factory from './factory';
import './index.css';
import * as messageBus from './messageBus';
import * as rxjs from 'rxjs';
import * as monacoAdapterImpl from './monacoAdapterImpl';

if (window) {

	window['postAndLog'] = (message: any) => {
		window.postMessage(message, '*');
		const messageLogger = window['postMessageLogger'];
		if (messageLogger) {
			messageLogger(message);
		}
	};
	const mainWindowMessages = new rxjs.Subject<messages.AnyApiMessage>();
	window.addEventListener('message', (event) => {
		const apiMessage = <messages.AnyApiMessage>event.data;
		if (apiMessage) {
			mainWindowMessages.next(apiMessage);
		}
	});

	const mainWindowMessageBus = new messageBus.MessageBus((message: messages.AnyApiMessage) => {
		window['postAndLog'](message);
	},
		mainWindowMessages
	);

	const editor = factory.createEditor(document.body);
	const kernel = factory.createWasmProjectKernel();
	const tdnEditor = new tryDotNetEditor.TryDotNetEditor(mainWindowMessageBus, kernel);



	tdnEditor.editor = new monacoAdapterImpl.MonacoEditorAdapter(editor);

	window['trydotnetEditor'] = tdnEditor;

	// for messaging api backward compatibility
	window['postAndLog']({
		type: "NOTIFY_HOST_EDITOR_READY"
	});
}
