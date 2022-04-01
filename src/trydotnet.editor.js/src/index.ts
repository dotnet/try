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

	const mainWindowMessages = new rxjs.Subject<messages.AnyApiMessage>();
	window.addEventListener('message', (event) => {
		const apiMessage = <messages.AnyApiMessage>event.data;
		if (apiMessage) {
			mainWindowMessages.next(apiMessage);
		}
	});

	const mainWindowMessageBus = new messageBus.MessageBus((message: messages.AnyApiMessage) => {
		window.postMessage(message);
	},
		mainWindowMessages
	);

	const editor = factory.createEditor(document.body);
	const kernel = factory.createWasmProjectKernel();
	const tdnEditor = new tryDotNetEditor.TryDotNetEditor(mainWindowMessageBus, kernel);

	tdnEditor.editor = new monacoAdapterImpl.MonacoEditorAdapter(editor);

	window['trydotnetEditor'] = tdnEditor;
}
