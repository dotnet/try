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

    commands.push(new polyglotNotebooks.KernelCommandEnvelope(
      polyglotNotebooks.OpenProjectType,
      <polyglotNotebooks.OpenProject>{ project: { ... this.openProject }, targetKernelName: rootCommand.command.targetKernelName }
    ));

    commands.push(rootCommand);

    let eventEnvelopes = await this._apiService(commands);
    commandInvocation.context;//?
    this.forwardEvents(eventEnvelopes, rootCommand, commandInvocation.context);
  }

  private async ensureProjectisLoadedAndForwardCommand(commandInvocation: polyglotNotebooks.IKernelCommandInvocation): Promise<void> {
    let rootCommand = commandInvocation.commandEnvelope;

    let commands: Array<polyglotNotebooks.KernelCommandEnvelope> = [];

    commands.push(new polyglotNotebooks.KernelCommandEnvelope(
      polyglotNotebooks.OpenProjectType,
      <polyglotNotebooks.OpenProject>{
        project: { ... this.openProject },
        targetKernelName: rootCommand.command.targetKernelName
      }
    ));

    commands.push(new polyglotNotebooks.KernelCommandEnvelope(
      polyglotNotebooks.OpenDocumentType,
      <polyglotNotebooks.OpenDocument>{
        relativeFilePath: this.openDocument.relativeFilePath,
        regionName: this.openDocument.regionName,
        targetKernelName: rootCommand.command.targetKernelName
      }
    ));

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
    const event = new polyglotNotebooks.KernelEventEnvelope(
      polyglotNotebooks.CodeSubmissionReceivedType,
      { code: (<polyglotNotebooks.SubmitCode>commandInvocation.commandEnvelope.command).code },
      commandInvocation.commandEnvelope

    );
    commandInvocation.context.publish(event);

    // original submitcode command
    let rootCommand = commandInvocation.commandEnvelope;

    let compileCommand = new polyglotNotebooks.KernelCommandEnvelope(
      polyglotNotebooks.CompileProjectType,
      <polyglotNotebooks.CompileProject>{
        targetKernelName: rootCommand.command.targetKernelName
      });

    let commands: Array<polyglotNotebooks.KernelCommandEnvelope> = [];

    commands.push(new polyglotNotebooks.KernelCommandEnvelope(
      polyglotNotebooks.OpenProjectType,
      <polyglotNotebooks.OpenProject>{ project: { ... this.openProject }, targetKernelName: rootCommand.command.targetKernelName }
    ));

    commands.push(new polyglotNotebooks.KernelCommandEnvelope(
      <polyglotNotebooks.KernelCommandType>polyglotNotebooks.OpenDocumentType,
      <polyglotNotebooks.OpenDocument>{
        relativeFilePath: this.openDocument.relativeFilePath,
        regionName: this.openDocument.regionName,
        targetKernelName: rootCommand.command.targetKernelName
      }
    ));

    commands.push(new polyglotNotebooks.KernelCommandEnvelope(
      <polyglotNotebooks.KernelCommandType>polyglotNotebooks.SubmitCodeType,
      <polyglotNotebooks.SubmitCode>{
        code: (<polyglotNotebooks.SubmitCode>rootCommand.command).code,
        targetKernelName: rootCommand.command.targetKernelName
      }
    ));

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
        const event = new polyglotNotebooks.KernelEventEnvelope(
          polyglotNotebooks.StandardOutputValueProducedType,
          <polyglotNotebooks.StandardOutputValueProduced>{
            formattedValues: [{
              mimeType: "text/plain",
              value: output
            }]
          },
          commandInvocation.commandEnvelope
        );
        polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner] handleSubmitCode - publish output event from wasm runner ${JSON.stringify(event)}`);
        commandInvocation.context.publish(event);
      },
      onError: (error: string) => {
        const event = new polyglotNotebooks.KernelEventEnvelope(
          polyglotNotebooks.StandardErrorValueProducedType,
          <polyglotNotebooks.StandardErrorValueProduced>{
            formattedValues: [{
              mimeType: "text/plain",
              value: error
            }]
          }, commandInvocation.commandEnvelope
        );
        polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner] handleSubmitCode - publish error event from was runnerm ${JSON.stringify(event)}`);
        commandInvocation.context.publish(event);
      }
    });
    polyglotNotebooks.Logger.default.info("[ProjectKernelWithWASMRunner] handleSubmitCode - done");
  }

  private forwardEvents(eventEnvelopes: Array<polyglotNotebooks.KernelEventEnvelope>, rootCommand: polyglotNotebooks.KernelCommandEnvelope, invocationContext: polyglotNotebooks.KernelInvocationContext) {
    for (let eventEnvelope of eventEnvelopes) {
      const eventType = eventEnvelope.eventType;

      if (eventType === polyglotNotebooks.CommandFailedType) {
        polyglotNotebooks.Logger.default.error(`[ProjectKernelWithWASMRunner] command failed: ${JSON.stringify(eventEnvelope)}`);
        throw new Error((<polyglotNotebooks.CommandFailed>(eventEnvelope.event)).message);
      }
      else if (eventType === polyglotNotebooks.CommandSucceededType) {
        continue;
      }
      else {
        // todo: do we need processing this?
        const event = polyglotNotebooks.KernelEventEnvelope.fromJson({
          ...eventEnvelope.toJson
            (),
          command: rootCommand.toJson()
        });
        polyglotNotebooks.Logger.default.info(`[ProjectKernelWithWASMRunner.forwardEvents] forwarding event from ApiService ${JSON.stringify(event)}`);
        invocationContext.publish(event);
      }
    }
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
