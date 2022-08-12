using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.TryDotNet.SimulatorGenerator;

internal record ApiContractScenario(string Label, KernelCommand[][] CommandBatches);