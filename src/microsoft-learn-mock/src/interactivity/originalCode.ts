import {
	loc_clear,
	loc_dotnetEditor,
	loc_dotnetEditor_acceleratorCtrlSpace,
	loc_dotnetEditor_acceleratorF1,
	loc_dotnetEditor_acceleratorShiftF10,
	loc_dotnetEditor_commonAcceleratorsAriaLabel,
	loc_dotnetEditor_exitEditorActionShortcut,
	loc_dotnetEditor_exitEditorActionShortcutMacOS,
	loc_feedback,
	loc_loading,
	loc_noOutput,
	loc_output,
	loc_run,
	loc_serviceUnavailable
} from '../docs-strings';
import { eventBus } from '../event-bus';
import { loadLibrary } from '../load-library';
import { currentTheme, ThemeChangedEvent, ThemeType } from '../theme-selection';
import { InteractiveComponent, registerInteractiveType } from './activation';
import { scaffoldCode, scaffoldingMethod } from './scaffolding';

const hostOrigin = location.origin;
const iconClass = 'docon docon-play';
const clearClass = 'docon docon-delete';


export function getUrls(): { trydotnetUrl: string, trydotnetOrigin: string } {
	const trydotnetUrl = '/static/third-party/trydotnet/0.1.1/trydotnet.js';
	const trydotnetOrigin = 'https://trydotnet.microsoft.com';

	let ret = {
		trydotnetUrl: trydotnetUrl,
		trydotnetOrigin: trydotnetOrigin
	}

	if (window) {
		let params = new URLSearchParams(window.location.search);
		ret.trydotnetUrl = params.get('trydotnetUrl') || trydotnetUrl;
		ret.trydotnetOrigin = params.get('trydotnetOrigin') || trydotnetOrigin;
	}
	return ret;
}

export class DotNetOnline implements InteractiveComponent {
	public readonly element: HTMLDivElement;
	public readonly ready: Promise<void>;
	private readonly runButton: HTMLButtonElement;
	private readonly clearButton: HTMLButtonElement;
	private readonly editor: HTMLIFrameElement;
	private readonly output: HTMLElement;
	private session?: DTOSession;
	private subscriptions?: Unsubscribable;
	private errorSubscription?: Unsubscribable;
	private runIsReady = false;
	private trydotnet?: TryDotnet;
	constructor(container: HTMLDivElement) {
		this.element = container;

		const [editorSection, serviceUnavailable, loader] = Array.from(this.element.children).map(
			x => x as HTMLElement
		);
		this.runButton = editorSection.querySelector('button[data-bi-name="tutorial-run-csharp"]')!;
		this.clearButton = editorSection.querySelector('button[data-bi-name="tutorial-clear-csharp"]')!;
		this.clearButton.onclick = () => this.clearEditor();
		this.runButton.onclick = () => this.execute();
		this.editor = editorSection.querySelector('iframe')!;
		this.output = editorSection.querySelector('pre')!;

		eventBus.subscribe(ThemeChangedEvent, e => {
			this.themeHandler(e.currentTheme);
		});

		this.ready = this.loadTryDotnet()
			.then(() => this.getEditorReady('HostEditorReady'))
			.then(() => {
				this.setTheme(currentTheme);
				loader.hidden = true;
				editorSection.hidden = false;
			})
			.catch(err => {
				loader.hidden = true;
				editorSection.hidden = true;
				serviceUnavailable.hidden = false;
				throw err;
			});
	}

	public async setCode(code: string, scaffoldingType?: string) {
		return this.ready.then(() => this.setCodeInternal(code, scaffoldingType));
	}

	public focus() {
		window.postMessage({ type: 'focusEditor' }, hostOrigin);
		return Promise.resolve();
	}

	public execute() {
		if (this.session) {
			this.runButton.classList.add('is-loading');
			this.output.classList.remove('error');
			this.output.textContent = '';

			// show is-loading indicator in output pane: "..."
			const interval = setInterval(() => {
				if (this.output) {
					this.output.textContent += '.';
					if (this.output.textContent && this.output.textContent.length > 3) {
						this.output.textContent = '';
					}
				}
			}, 200);
			this.subscriptions = this.session.subscribeToOutputEvents((event: OutputEvent) => {
				clearInterval(interval); // terminate is-loading indicator animation
				this.runButton.classList.remove('is-loading');
				if (event.exception) {
					this.output.classList.add('error');
					this.output.textContent = event.exception.join('\n') as string;
				} else if (event.stdout) {
					this.output.classList.remove('error');
					let output = event.stdout.join('\n');
					if (output.length === 0) {
						output = loc_noOutput;
					}
					this.output.textContent = output;
				} else {
					throw new Error(
						`Unexpected run result: ${(this as Omit<DotNetOnline, 'output'> & { output: string }).output
						}`
					);
				}
			});
			this.errorSubscription = this.session.subscribeToServiceErrorEvents((event: ServiceError) => {
				clearInterval(interval);

				this.output.classList.add('error');
				this.output.textContent = loc_serviceUnavailable;

				/*eslint-disable-next-line */
				console.error(event.message);
			});

			this.session.onCanRunChanged(ready => {
				this.runIsReady = ready;
			});
			this.runWhenReady();
		}
	}

	public dispose() {
		this.subscriptions?.unsubscribe();
		this.errorSubscription?.unsubscribe();
	}

	private clearEditor() {
		if (this.session) {
			const content = this.session.getTextEditor();
			content?.setContent('');
		}
	}

	private async loadTryDotnet() {
		let settings = getUrls();
		this.trydotnet = await loadLibrary<TryDotnet>(
			settings.trydotnetUrl,
			'sha384-vt3e73ZrS44C2kkryuZnkklKXXk5wjXYTDwIDvYz10+iEP1dES/uqoRqrwhIPruS',
			'trydotnet'
		);
		if (!this.trydotnet) {
			this.output.classList.add('error');
			this.output.textContent = loc_serviceUnavailable;
		}
	}

	private async getEditorReady(type: string) {
		if (this.trydotnet) {
			let settings = getUrls();
			//window.postMessage({ type, editorId: "0" }, hostOrigin);
			const configuration: Configuration = { hostOrigin, trydotnetOrigin: settings.trydotnetOrigin, enableLogging: true };
			const content = scaffoldingMethod.replace('____', '');
			const fileName: string = 'program.cs';
			const files: SourceFile[] = [{ name: fileName, content }];
			const project: Project = { package: 'console', files };
			const document: any = { fileName, region: 'controller' };
			const awaitableSession = this.trydotnet.createSessionWithProjectAndOpenDocument(
				configuration,
				[this.editor],
				window,
				project,
				document
			);
			this.session = await awaitableSession;
			return this.session;
		}
		else {
			throw new Error('TryDotnet is not loaded');
		}
	}

	private themeHandler = (docsTheme: ThemeType) => {
		this.setTheme(docsTheme);
	};

	private setTheme(docsTheme: ThemeType) {
		if (this.session) {
			const docsToMonacoThemeMap = {
				light: 'vs-light',
				dark: 'vs-dark',
				'high-contrast': 'hc-black'
			};

			const theme = docsToMonacoThemeMap[docsTheme];
			const defaultEditor = this.session.getTextEditor();
			defaultEditor.setTheme(theme);
		}
	}

	private async setCodeInternal(code: string, scaffoldingType?: string) {
		if (this.session && this.trydotnet) {
			code = scaffoldCode(code, scaffoldingType ?? 'try-dotnet-method');
			const fileName: string = 'program.cs';
			const files: SourceFile[] = [{ name: fileName, content: code }];
			const project = await this.trydotnet.createProject({ packageName: 'console', files });
			await this.session.openProject(project);
			const defaultEditor = this.session.getTextEditor();
			let region: string | undefined;
			if (scaffoldingType !== 'try-dotnet') {
				region = 'controller';
			}
			await this.session.openDocument({ fileName, editorId: defaultEditor.id(), region });
		}
	}

	// We are not sure when all resources are
	// loaded for the first time, so we need
	// to wait until editor is ready
	// If editor ready, then run.
	private runWhenReady() {

		if (this.runIsReady && this.session) {
			this.session.run();
		} else {
			setTimeout(() => {
				this.runWhenReady();
			}, 200);
		}
	}
}


// registerInteractiveType({
// 	name: 'csharp',
// 	activateButtonConfig: {
// 		name: loc_run,
// 		iconClass,
// 		attributes: []
// 	},
// 	create: () => new DotNetOnline()
// });

interface Unsubscribable {
	unsubscribe(): void;
}
interface TryDotnet {
	createProject(args: {
		packageName: string;
		files: SourceFile[];
		usings?: string[];
		language?: string;
	}): Promise<Project>;
	createSessionWithProjectAndOpenDocument(
		configuration: Configuration,
		editorIFrames: HTMLIFrameElement[],
		window: Window,
		project: Project,
		document: Document,
		documentsToInclude?: DocumentObject[]
	): Promise<DTOSession>;
}
interface DocumentObject {
	fileName: string;
	region: Region;
	content: string;
}
declare type Region = string;
interface Configuration {
	hostOrigin?: string;
	trydotnetOrigin?: string;
	enableLogging?: boolean
}

interface DTODocument {
	id(): string;
	setContent(content: string): Promise<void>;
	getContent(): string;
}
interface OpenDocumentParameters {
	fileName: string;
	region?: Region;
	editorId?: string;
	content?: string;
}

interface DTOTextDisplay {
	setContent(content: string): Promise<void>;
	id(): string;
}

interface DTOTextEditor extends DTOTextDisplay {
	textChanges: any;
	setTheme(theme: string): void;
}
interface RunConfiguration {
	instrument?: boolean;
	runWorkflowId?: string;
	runArgs?: string;
}
interface Diagnostic {
	start: number;
	end: number;
	message: string;
	severity: number;
}
interface RunResult {
	runId: string;
	succeeded: boolean;
	diagnostics?: Diagnostic[];
	output?: string[];
	exception?: any;
}
type OutputEventSubscriber = (event: OutputEvent) => void;
type ServiceErrorSubscriber = (error: ServiceError) => void;

interface DTOSession {
	//getOpenDocuments(): DTODocument[]; deleted
	openProject(project: Project): Promise<void>;
	openDocument(parameters: OpenDocumentParameters): Promise<DTODocument>;
	getTextEditor(): DTOTextEditor;
	run(configuration?: RunConfiguration): Promise<RunResult>;
	subscribeToOutputEvents(handler: OutputEventSubscriber): Unsubscribable;
	subscribeToServiceErrorEvents(handler: ServiceErrorSubscriber): Unsubscribable;
	onCanRunChanged(changed: (canRun: boolean) => void): void;
}
interface OutputEvent {
	stdout?: string[];
	exception?: string[];
}
interface Project {
	[key: string]: any;
	package: string;
	packageVersion?: string;
	language?: string;
	files: SourceFile[];
}
interface SourceFile {
	name: string;
	content: string;
}
interface ServiceError {
	statusCode: string;
	message: string;
	requestId: string;
}