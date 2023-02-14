// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as polyglotNotebooks from '@microsoft/polyglot-notebooks';

class CommandOperation {
    constructor(public readonly kernelCommandEnvelope: polyglotNotebooks.KernelCommandEnvelope, public readonly completionSource: polyglotNotebooks.PromiseCompletionSource<void>) {

    }
    complete() {
        this.completionSource.resolve();
    }

    cancel() {
        this.completionSource.resolve();
    }
    fail(reason: any) {
        this.completionSource.reject(reason);
    }
}

export class DebouncingKernel {

    private _langiageServicesCommandOperation: CommandOperation = null;
    private _commandOperations: CommandOperation[] = [];
    private _running: boolean = false;

    constructor(private _kernel: polyglotNotebooks.Kernel) {
        if (!_kernel) {
            throw new Error("kernel is null");
        }
    }

    public asInteractiveKernel(): polyglotNotebooks.Kernel {
        return <polyglotNotebooks.Kernel><any>this;
    }

    get kernelInfo(): polyglotNotebooks.KernelInfo {
        return this._kernel.kernelInfo;
    }

    public subscribeToKernelEvents(observer: polyglotNotebooks.KernelEventEnvelopeObserver): polyglotNotebooks.DisposableSubscription {
        return this._kernel.subscribeToKernelEvents(observer);
    }

    public send(commandEnvelope: polyglotNotebooks.KernelCommandEnvelope): Promise<void> {
        const newOperation: CommandOperation = new CommandOperation(commandEnvelope, new polyglotNotebooks.PromiseCompletionSource());
        switch (commandEnvelope.commandType) {
            case polyglotNotebooks.RequestCompletionsType:
            case polyglotNotebooks.RequestHoverTextType:
            case polyglotNotebooks.RequestSignatureHelpType:
                this._langiageServicesCommandOperation?.cancel();
                this._langiageServicesCommandOperation = newOperation;
                break;
            case polyglotNotebooks.RequestDiagnosticsType:
                if (this._langiageServicesCommandOperation) {
                    switch (this._langiageServicesCommandOperation.kernelCommandEnvelope.commandType) {
                        case polyglotNotebooks.RequestDiagnosticsType:
                            this._langiageServicesCommandOperation?.cancel();
                            this._langiageServicesCommandOperation = newOperation;
                            break;
                        default:
                            this._commandOperations.push(newOperation);
                            break;
                    }
                } else {
                    this._langiageServicesCommandOperation = newOperation;
                }
                break;
            default:
                this._commandOperations.push(newOperation);
                break;
        }

        if (!this._running) {
            this.executionLoop();
        }

        return newOperation.completionSource.promise;
    }
    async executionLoop() {
        this._running = true;

        while (this._langiageServicesCommandOperation || this._commandOperations.length > 0) {
            {
                if (this._langiageServicesCommandOperation) {
                    const local = this._langiageServicesCommandOperation;
                    this._langiageServicesCommandOperation = null;
                    await this._kernel.send(local.kernelCommandEnvelope).then(() => {
                        local.complete();
                    }).catch(r => local.completionSource.reject(r));
                } else if (this._commandOperations.length > 0) {
                    const local = this._commandOperations.shift();
                    await this._kernel.send(local.kernelCommandEnvelope).then(() => {
                        local.complete();
                    }).catch(r => local.fail(r));
                }
            }
        }

        this._running = false;
    }

}