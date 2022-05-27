// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as dotnetInteractive from '@microsoft/dotnet-interactive';
import { Logger } from '@microsoft/dotnet-interactive';
import * as apiService from './apiService';
import * as projectKernel from './projectKernel';
import * as runner from './wasmRunner';

export class ProjectKernelWithWASMRunner extends projectKernel.ProjectKernel {

  private _tokenSeed = 0;
  protected async handleOpenProject(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {
    let rootCommand = commandInvocation.commandEnvelope;

    let commands: Array<dotnetInteractive.KernelCommandEnvelope> = [rootCommand];

    let eventEnvelopes = await this._apiService(commands);
    commandInvocation.context;//?
    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);

  }

  protected async handleOpenDocument(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {
    let rootCommand = commandInvocation.commandEnvelope;

    let commands: Array<dotnetInteractive.KernelCommandEnvelope> = [];

    commands.push({
      commandType: dotnetInteractive.OpenProjectType,
      command: <dotnetInteractive.OpenProject>{ project: { ... this.openProject } },
      token: this.deriveToken(rootCommand)
    });

    commands.push(rootCommand);

    let eventEnvelopes = await this._apiService(commands);
    commandInvocation.context;//?
    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);
  }

  private async ensureProjectisLoadedAndForwardCommand(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {
    let rootCommand = commandInvocation.commandEnvelope;

    let commands: Array<dotnetInteractive.KernelCommandEnvelope> = [];

    commands.push({
      commandType: dotnetInteractive.OpenProjectType,
      command: <dotnetInteractive.OpenProject>{ project: { ... this.openProject } },
      token: this.deriveToken(rootCommand)
    });

    commands.push({
      commandType: dotnetInteractive.OpenDocumentType,
      command: <dotnetInteractive.OpenDocument>{
        relativeFilePath: this.openDocument.relativeFilePath,
        regionName: this.openDocument.regionName
      },
      token: this.deriveToken(rootCommand)
    });

    commands.push(rootCommand);

    let eventEnvelopes = await this._apiService(commands);

    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);
  }

  protected async handleRequestDiagnostics(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleRequestCompletions(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleRequestHoverText(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleRequestSignatureHelp(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleSubmitCode(commandInvocation: dotnetInteractive.IKernelCommandInvocation): Promise<void> {

    dotnetInteractive.Logger.default.info("[ProjectKernelWithWASMRunner] handleSubmitCode - start");
    commandInvocation.context.publish({
      eventType: dotnetInteractive.CodeSubmissionReceivedType,
      event: { code: (<dotnetInteractive.SubmitCode>commandInvocation.commandEnvelope.command).code },
      command: commandInvocation.commandEnvelope
    });

    // original submitcode command
    let rootCommand = commandInvocation.commandEnvelope;

    let compileCommand: dotnetInteractive.KernelCommandEnvelope = {
      commandType: dotnetInteractive.CompileProjectType,
      command: <dotnetInteractive.CompileProject>{
      },
      token: rootCommand.token
    };

    let commands: Array<dotnetInteractive.KernelCommandEnvelope> = [];

    commands.push({
      commandType: dotnetInteractive.OpenProjectType,
      command: <dotnetInteractive.OpenProject>{ project: { ... this.openProject } },
      token: this.deriveToken(compileCommand)
    });

    commands.push({
      commandType: <dotnetInteractive.KernelCommandType>dotnetInteractive.OpenDocumentType,
      command: <dotnetInteractive.OpenDocument>{
        relativeFilePath: this.openDocument.relativeFilePath,
        regionName: this.openDocument.regionName
      },
      token: this.deriveToken(compileCommand)
    });

    commands.push({
      commandType: <dotnetInteractive.KernelCommandType>dotnetInteractive.SubmitCodeType,
      command: <dotnetInteractive.SubmitCode>{
        code: (<dotnetInteractive.SubmitCode>rootCommand.command).code
      },
      token: this.deriveToken(compileCommand)
    });

    commands.push(compileCommand);

    Logger.default.info(`[ProjectKernelWithWASMRunner] commands: ${JSON.stringify(commands)}`);
    let eventEnvelopes = await this._apiService(commands);
    Logger.default.info(`[ProjectKernelWithWASMRunner] events: ${JSON.stringify(eventEnvelopes)}`);

    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);

    let assemblyProduced = eventEnvelopes.find(e => e.eventType === dotnetInteractive.AssemblyProducedType);
    dotnetInteractive.Logger.default.info("[ProjectKernelWithWASMRunner] handleSubmitCode - wasmrunner");

    const assembly = (<dotnetInteractive.AssemblyProduced>assemblyProduced.event).assembly;

    dotnetInteractive.Logger.default.info(`[ProjectKernelWithWASMRunner]  assembly to run : ${JSON.stringify(assemblyProduced)}`);

    await this._wasmRunner({
      assembly: assembly,
      onOutput: (output: string) => {
        const event: dotnetInteractive.KernelEventEnvelope = {
          eventType: dotnetInteractive.StandardOutputValueProducedType,
          event: <dotnetInteractive.StandardOutputValueProduced>{
            formattedValues: [{
              mimeType: "text/plain",
              value: output
            }]
          },
          command: commandInvocation.commandEnvelope
        };
        dotnetInteractive.Logger.default.info("handleSubmitCode - publish event");
        commandInvocation.context.publish(event);
      },
      onError: (error: string) => {
        const event: dotnetInteractive.KernelEventEnvelope = {
          eventType: dotnetInteractive.StandardErrorValueProducedType,
          event: <dotnetInteractive.StandardErrorValueProduced>{
            formattedValues: [{
              mimeType: "text/plain",
              value: error
            }]
          },
          command: commandInvocation.commandEnvelope
        };
        dotnetInteractive.Logger.default.info("handleSubmitCode - publish event");
        commandInvocation.context.publish(event);
      }
    });
    dotnetInteractive.Logger.default.info("handleSubmitCode - done");
  }

  private forwardEvents(eventEnvelopes: Array<dotnetInteractive.KernelEventEnvelope>, rootCommand: dotnetInteractive.KernelCommandEnvelope, invocationContext: dotnetInteractive.KernelInvocationContext) {
    for (let eventEnvelope of eventEnvelopes) {
      if (eventEnvelope.eventType === dotnetInteractive.CommandFailedType) {
        throw new Error((<dotnetInteractive.CommandFailed>(eventEnvelope.event)).message);
      }
      else if (eventEnvelope.eventType === dotnetInteractive.CommandCancelledType) {
        throw new Error("Command cancelled");
      }
      else if (eventEnvelope.eventType === dotnetInteractive.CommandSucceededType
        && eventEnvelope.command.token === rootCommand.token) {
        if (eventEnvelope.command.commandType === rootCommand.commandType) {
          break;
        }
        else {
          continue;
        }
      }
      else if (eventEnvelope.command.commandType === rootCommand.commandType && eventEnvelope.command.token === rootCommand.token) {
        // todo: do we need processing this?
        invocationContext.publish({ ...eventEnvelope, command: rootCommand });
      }
    }
  }

  private deriveToken(originalCommand: dotnetInteractive.KernelCommandEnvelope) {
    return `${originalCommand.token}||${this._tokenSeed++}`;
  }

  constructor(kernelName: string, private _wasmRunner: runner.IWasmRunner, private _apiService: apiService.IApiService) {
    super(kernelName);
    if (!this._apiService) {
      throw new Error("apiService is required");
    }

    if (!this._wasmRunner) {
      throw new Error("wasmRunner is required");
    }
  }
}
