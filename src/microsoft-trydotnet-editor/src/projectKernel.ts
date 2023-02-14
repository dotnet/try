// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';

export abstract class ProjectKernel extends polyglotNotebooks.Kernel {
  private _project: polyglotNotebooks.Project;
  private _openDocument?: polyglotNotebooks.OpenDocument;

  protected get openProject() {
    return this._project;
  }

  protected get openDocument(): {
    relativeFilePath: string;
    regionName?: string
  } {
    return this._openDocument;
  }

  constructor(kernelName: string) {
    super(kernelName);


    this.registerCommandHandler({
      commandType: polyglotNotebooks.SubmitCodeType,
      handle: (commandInvocation: polyglotNotebooks.IKernelCommandInvocation) => {

        this.throwIfProjectIsNotOpened();
        this.throwIffDocumentIsNotOpened();
        return this.handleSubmitCode(commandInvocation);
      }
    });

    this.registerCommandHandler({
      commandType: polyglotNotebooks.OpenProjectType,
      handle: async (commandInvocation: polyglotNotebooks.IKernelCommandInvocation) => {
        await this.handleOpenProject(commandInvocation);
        let command = <polyglotNotebooks.OpenProject>commandInvocation.commandEnvelope.command;
        this._project = JSON.parse(JSON.stringify(command.project));
      }
    });

    this.registerCommandHandler({
      commandType: polyglotNotebooks.OpenDocumentType,
      handle: async (commandInvocation: polyglotNotebooks.IKernelCommandInvocation) => {
        this.throwIfProjectIsNotOpened();
        await this.handleOpenDocument(commandInvocation);
        let command = <polyglotNotebooks.OpenDocument>commandInvocation.commandEnvelope.command;
        this._openDocument = {
          relativeFilePath: command.relativeFilePath
        };

        if (command.regionName) {
          this._openDocument.regionName = command.regionName;
        }
      }
    });

    this.registerCommandHandler({
      commandType: polyglotNotebooks.RequestDiagnosticsType,
      handle: (commandInvocation: polyglotNotebooks.IKernelCommandInvocation) => {
        this.throwIfProjectIsNotOpened();
        this.throwIffDocumentIsNotOpened();
        return this.handleRequestDiagnostics(commandInvocation);
      }
    });

    this.registerCommandHandler({
      commandType: polyglotNotebooks.RequestCompletionsType,
      handle: (commandInvocation: polyglotNotebooks.IKernelCommandInvocation) => {
        this.throwIfProjectIsNotOpened();
        this.throwIffDocumentIsNotOpened();
        return this.handleRequestCompletions(commandInvocation);
      }
    });

    this.registerCommandHandler({
      commandType: polyglotNotebooks.RequestHoverTextType,
      handle: (commandInvocation: polyglotNotebooks.IKernelCommandInvocation) => {
        this.throwIfProjectIsNotOpened();
        this.throwIffDocumentIsNotOpened();
        return this.handleRequestHoverText(commandInvocation);
      }
    });

    this.registerCommandHandler({
      commandType: polyglotNotebooks.RequestSignatureHelpType,
      handle: (commandInvocation: polyglotNotebooks.IKernelCommandInvocation) => {
        this.throwIfProjectIsNotOpened();
        this.throwIffDocumentIsNotOpened();
        return this.handleRequestSignatureHelp(commandInvocation);
      }
    });
  }

  protected throwIfProjectIsNotOpened() {
    if (!this._project) {
      // todo : align error message with .NET
      throw new Error(`Project must be opened, send the command '${polyglotNotebooks.OpenProjectType}' first.`);
    }
  }

  protected throwIffDocumentIsNotOpened() {
    if (!this._openDocument) {
      // todo : align error message with .NET
      throw new Error(`Document must be opened, send the command '${polyglotNotebooks.OpenDocumentType}' first.`);
    }
  }

  protected abstract handleOpenProject(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestDiagnostics(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestCompletions(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestHoverText(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestSignatureHelp(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void>;
  protected abstract handleSubmitCode(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void>;
  protected abstract handleOpenDocument(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void>;
}
