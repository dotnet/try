using System.CommandLine;
using Microsoft.TryDotNet.SimulatorGenerator;

var existingOnlyOption = new Option<DirectoryInfo>("--destination-folder")
{
    Description = "Location to write the simulator files",
    IsRequired = true
}.AcceptExistingOnly();

var command = new RootCommand
{
    existingOnlyOption
};

command.SetHandler(async (DirectoryInfo destinationFolder) =>
{
    
    await ApiEndpointSimulatorGenerator.CreateScenarioFiles(destinationFolder);
}, existingOnlyOption);

return command.Invoke(args);
