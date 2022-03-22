// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';

export abstract class ProjectKernel extends dotnetInteractive.Kernel {
  private _project: dotnetInteractive.Project;
  private _openDocument?: dotnetInteractive.OpenDocument;

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
      commandType: dotnetInteractive.SubmitCodeType,
      handle: (commandInvocation: dotnetInteractive.IKernelCommandInvocation) => {
        if (!this._project) {
          // todo : align error message with .NET
          throw new Error("Project is not loaded");
        }
        if (!this._openDocument) {
          // todo : align error message with .NET
          throw new Error("No Open document found");
        }
        return this.handleSubmitCode(commandInvocation);
      }
    });

    this.registerCommandHandler({
      // why???
      commandType: dotnetInteractive.OpenProjectType,
      handle: async (commandInvocation: dotnetInteractive.IKernelCommandInvocation) => {
        await this.handleOpenProject(commandInvocation);
        let command = <dotnetInteractive.OpenProject>commandInvocation.commandEnvelope.command;
        this._project = JSON.parse(JSON.stringify(command.project));
      }
    });

    this.registerCommandHandler({
      commandType: dotnetInteractive.OpenDocumentType,
      handle: async (commandInvocation: dotnetInteractive.IKernelCommandInvocation) => {
        if (!this._project) {
          // todo : align error message with .NET
          throw new Error("Project is not loaded");
        }
        await this.handleOpenDocument(commandInvocation);
        let command = <dotnetInteractive.OpenDocument>commandInvocation.commandEnvelope.command;
        this._openDocument = {
          relativeFilePath: command.relativeFilePath,
          regionName: command.regionName
        };
      }
    });

    this.registerCommandHandler({
      commandType: dotnetInteractive.RequestDiagnosticsType,
      handle: (commandInvocation: dotnetInteractive.IKernelCommandInvocation) => {
        if (!this._project) {
          // todo : align error message with .NET
          throw new Error("Project is not loaded");
        }
        if (!this._openDocument) {
          // todo : align error message with .NET
          throw new Error("No Open document found");
        }
        return this.handleRequestDiagnostics(commandInvocation);
      }
    });

    this.registerCommandHandler({
      commandType: dotnetInteractive.RequestCompletionsType,
      handle: (commandInvocation: dotnetInteractive.IKernelCommandInvocation) => {
        if (!this._project) {
          // todo : align error message with .NET
          throw new Error("Project is not loaded");
        }
        if (!this._openDocument) {
          // todo : align error message with .NET
          throw new Error("No Open document found");
        }
        return this.handleRequestCompletions(commandInvocation);
      }
    });

    this.registerCommandHandler({
      commandType: dotnetInteractive.RequestHoverTextType,
      handle: (commandInvocation: dotnetInteractive.IKernelCommandInvocation) => {
        if (!this._project) {
          // todo : align error message with .NET
          throw new Error("Project is not loaded");
        }
        if (!this._openDocument) {
          // todo : align error message with .NET
          throw new Error("No Open document found");
        }
        return this.handleRequestHoverText(commandInvocation);
      }
    });

    this.registerCommandHandler({
      commandType: dotnetInteractive.RequestSignatureHelpType,
      handle: (commandInvocation: dotnetInteractive.IKernelCommandInvocation) => {
        if (!this._project) {
          // todo : align error message with .NET
          throw new Error("Project is not loaded");
        }
        if (!this._openDocument) {
          // todo : align error message with .NET
          throw new Error("No Open document found");
        }
        return this.handleRequestSignatureHelp(commandInvocation);
      }
    });
  }


  protected abstract handleOpenProject(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestDiagnostics(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestCompletions(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestHoverText(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void>;
  protected abstract handleRequestSignatureHelp(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void>;
  protected abstract handleSubmitCode(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void>;
  protected abstract handleOpenDocument(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void>;
}

