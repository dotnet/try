export interface InteractiveComponent {
    element: Element;
    /**
     * Called at activation - will be passed the text content from the associated code block.
     */
    setCode: (code: string, scaffoldingType?: string) => Promise<void>;
    /**
     * Execution code called when the "Try It" button is clicked.
     */
    execute: () => void;
    dispose: () => void;
}

export interface RegisterInteractiveTypeArgs {
    name: InteractiveType;
    activateButtonConfig: ActivateButtonConfig;
    create: () => InteractiveComponent;
}

export type InteractiveType =
    | 'bash'
    | 'csharp'
    | 'http'
    | 'powershell'
    | 'lab-on-demand'
    | 'msgraph';

export interface ActivateButtonConfig {
    name: string;
    iconClass: string;
    attributes: { name: string; value: string }[];
}

export interface RegisterInteractiveTypeArgs {
    name: InteractiveType;
    activateButtonConfig: ActivateButtonConfig;
    create: () => InteractiveComponent;
}

const interactiveTypes: { [name: string]: RegisterInteractiveTypeArgs } = {};

export function registerInteractiveType(interactiveType: RegisterInteractiveTypeArgs) {
    interactiveTypes[interactiveType.name] = interactiveType;
}