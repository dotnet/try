dotnet build ../Microsoft.TryDotNet.SimulatorGenerator -o ./simulatorGenerator/
cd ./simulatorGenerator/
dotnet Microsoft.TryDotNet.SimulatorGenerator.dll --destination-folder ../tests/simulatorConfigurations/apiService
cd ..