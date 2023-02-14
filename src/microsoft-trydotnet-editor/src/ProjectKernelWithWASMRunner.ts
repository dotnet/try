// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';
import * as apiService from './apiService';
import * as projectKernel from './projectKernel';
import * as runner from './wasmRunner';

export class ProjectKernelWithWASMRunner extends projectKernel.ProjectKernel {

  private _tokenSeed = 0;
  protected async handleOpenProject(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    let rootCommand = commandInvocation.commandEnvelope;

    let commands: Array<polyglotNotebooks.KernelCommandEnvelope> = [rootCommand];

    let eventEnvelopes = await this._apiService(commands);
    commandInvocation.context;//?
    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);

  }

  protected async handleOpenDocument(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    let rootCommand = commandInvocation.commandEnvelope;

    let commands: Array<polyglotNotebooks.KernelCommandEnvelope> = [];

    commands.push({
      commandType: polyglotNotebooks.OpenProjectType,
      command: <polyglotNotebooks.OpenProject>{ project: { ... this.openProject }, targetKernelName: rootCommand.command.targetKernelName },
      token: this.deriveToken(rootCommand),

    });

    commands.push(rootCommand);

    let eventEnvelopes = await this._apiService(commands);
    commandInvocation.context;//?
    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);
  }

  private async ensureProjectisLoadedAndForwardCommand(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    let rootCommand = commandInvocation.commandEnvelope;

    let commands: Array<polyglotNotebooks.KernelCommandEnvelope> = [];

    commands.push({
      commandType: polyglotNotebooks.OpenProjectType,
      command: <polyglotNotebooks.OpenProject>{
        project: { ... this.openProject },
        targetKernelName: rootCommand.command.targetKernelName
      },
      token: this.deriveToken(rootCommand)
    });

    commands.push({
      commandType: polyglotNotebooks.OpenDocumentType,
      command: <polyglotNotebooks.OpenDocument>{
        relativeFilePath: this.openDocument.relativeFilePath,
        regionName: this.openDocument.regionName,
        targetKernelName: rootCommand.command.targetKernelName
      },
      token: this.deriveToken(rootCommand)
    });

    commands.push(rootCommand);

    let eventEnvelopes = await this._apiService(commands);

    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);
  }

  protected async handleRequestDiagnostics(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleRequestCompletions(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleRequestHoverText(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleRequestSignatureHelp(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    await this.ensureProjectisLoadedAndForwardCommand(commandInvocation);
  }

  protected async handleSubmitCode(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {

    polyglotNotebooks.Logger.default.info("[ProjectKernelWithWASMRunner] handleSubmitCode - start");
    commandInvocation.context.publish({
      eventType: polyglotNotebooks.CodeSubmissionReceivedType,
      event: { code: (<polyglotNotebooks.SubmitCode>commandInvocation.commandEnvelope.command).code },
      command: commandInvocation.commandEnvelope
    });

    // original submitcode command
    let rootCommand = commandInvocation.commandEnvelope;

    let compileCommand: polyglotNotebooks.KernelCommandEnvelope = {
      commandType: polyglotNotebooks.CompileProjectType,
      command: <polyglotNotebooks.CompileProject>{
        targetKernelName: rootCommand.command.targetKernelName
      },
      token: rootCommand.token
    };

    let commands: Array<polyglotNotebooks.KernelCommandEnvelope> = [];

    commands.push({
      commandType: polyglotNotebooks.OpenProjectType,
      command: <polyglotNotebooks.OpenProject>{ project: { ... this.openProject }, targetKernelName: rootCommand.command.targetKernelName },
      token: this.deriveToken(compileCommand)
    });

    commands.push({
      commandType: <polyglotNotebooks.KernelCommandType>polyglotNotebooks.OpenDocumentType,
      command: <polyglotNotebooks.OpenDocument>{
        relativeFilePath: this.openDocument.relativeFilePath,
        regionName: this.openDocument.regionName,
        targetKernelName: rootCommand.command.targetKernelName
      },
      token: this.deriveToken(compileCommand)
    });

    commands.push({
      commandType: <polyglotNotebooks.KernelCommandType>polyglotNotebooks.SubmitCodeType,
      command: <polyglotNotebooks.SubmitCode>{
        code: (<polyglotNotebooks.SubmitCode>rootCommand.command).code,
        targetKernelName: rootCommand.command.targetKernelName
      },
      token: this.deriveToken(compileCommand)
    });

    commands.push(compileCommand);

    polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner] commands: ${JSON.stringify(commands)}`);
    let eventEnvelopes = await this._apiService(commands);
    polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner] events: ${JSON.stringify(eventEnvelopes)}`);

    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);

    let assemblyProduced = eventEnvelopes.find(e => e.eventType === polyglotNotebooks.AssemblyProducedType);
    polyglotNotebooks.Logger.default.info("[ProjectKernelWithWASMRunner] handleSubmitCode - wasmrunner");

    const assembly = (<polyglotNotebooks.AssemblyProduced>assemblyProduced.event).assembly;

    polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner]  assembly to run : ${JSON.stringify(assemblyProduced)}`);

    await this._wasmRunner({
      assembly: assembly,
      onOutput: (output: string) => {
        const event: polyglotNotebooks.KernelEventEnvelope = {
          eventType: polyglotNotebooks.StandardOutputValueProducedType,
          event: <polyglotNotebooks.StandardOutputValueProduced>{
            formattedValues: [{
              mimeType: "text/plain",
              value: output
            }]
          },
          command: commandInvocation.commandEnvelope
        };
        polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner] handleSubmitCode - publish output event from wasm runner ${JSON.stringify(event)}`);
        commandInvocation.context.publish(event);
      },
      onError: (error: string) => {
        const event: polyglotNotebooks.KernelEventEnvelope = {
          eventType: polyglotNotebooks.StandardErrorValueProducedType,
          event: <polyglotNotebooks.StandardErrorValueProduced>{
            formattedValues: [{
              mimeType: "text/plain",
              value: error
            }]
          },
          command: commandInvocation.commandEnvelope
        };
        polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner] handleSubmitCode - publish error event from was runnerm ${JSON.stringify(event)}`);
        commandInvocation.context.publish(event);
      }
    });
    polyglotNotebooks.Logger.default.info("[ProjectKernelWithWASMRunner] handleSubmitCode - done");
  }

  private forwardEvents(eventEnvelopes: Array<polyglotNotebooks.KernelEventEnvelope>, rootCommand: polyglotNotebooks.KernelCommandEnvelope, invocationContext: polyglotNotebooks.KernelInvocationContext) {
    for (let eventEnvelope of eventEnvelopes) {
      if (eventEnvelope.eventType === polyglotNotebooks.CommandFailedType) {
        polyglotNotebooks.Logger.default.error(`[ProjectKernelWithWASMRunner] command failed: ${JSON.stringify(eventEnvelope)}`);
        throw new Error((<polyglotNotebooks.CommandFailed>(eventEnvelope.event)).message);
      }
      else if (eventEnvelope.eventType === polyglotNotebooks.CommandCancelledType) {
        throw new Error("Command cancelled");
      }
      else if (eventEnvelope.eventType === polyglotNotebooks.CommandSucceededType
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
        const event = { ...eventEnvelope, command: rootCommand };
        polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner.forwardEvents] forwarding event from ApiService ${JSON.stringify(event)}`);
        invocationContext.publish(event);
      }
    }
  }

  private deriveToken(originalCommand: polyglotNotebooks.KernelCommandEnvelope) {
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
