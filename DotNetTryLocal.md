# Getting started with dotnet try 
## Setup
Before you get can started creating interactive documentation, you will need to install the following: 

- The [.NET Core 3.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0) 
- The [.NET Core 2.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/2.1) 
- The [dotnet try](https://www.nuget.org/packages/Microsoft.dotnet-try/)  global tool

Updating to the latest version of the tool is easy. Run the following command:

```console
> dotnet tool update -g Microsoft.dotnet-try
```

## Installing preview builds from `master`

To install the latest preview build from master, first uninstall the existing version of the tool:

```console
> dotnet tool uninstall -g Microsoft.dotnet-try
```
Then, install from the preview package feed:

```console
> dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" Microsoft.dotnet-try
```

## Getting Started

You can get started using either one of the options below. 

### Option1: `dotnet try demo` 

- Create a new folder.
- `cd` to your new folder.
- Run command `dotnet try demo` : This will load our interactive dotnet try getting started tutorial. 

The tutorials below work you through the following:

- Creating a new Try .NET project.
- Display interactive snippets using C# regions.
- Creating Sessions
- Verifying your projects: `dotnet try verify` a compiler for your documentation.
- Passing Arguments
- Using read only snippets

### Option 2: Starting from scratch.
1. Go to the terminal and create a folder called `mydoc`.
2. `cd` to the `mydoc` folder and create a new console app with the following command:

```console
> dotnet new console -o myApp
```
This will create a console app with the files `myApp.csproj` and `Program.cs`.

3. Open the `mydoc` folder in Visual Studio Code. 

4. Create a file called `doc.md`. Inside that file, add some text and a code fence:

````markdown
# My Interactive Document:

```cs --source-file ./myApp/Program.cs --project ./myApp/myApp.csproj
```
````
5. Now, navigate to the `mydoc` folder in your console and run the following command:
```console
     > dotnet try
```
You have created your first C# interactive developer experience. You should now be able to run your console app and see the result in the browser. 

### Option 3: Explore our [samples repo](https://github.com/dotnet/try-samples). 
- Clone the [dotnet/try-samples](https://github.com/dotnet/try-samples) repo.
- Follow the quick steps listed [here](https://github.com/dotnet/try-samples#basics) to get started.

Return to [README.md](README.md)
