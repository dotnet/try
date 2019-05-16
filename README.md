# Try .NET <img src ="https://user-images.githubusercontent.com/2546640/56708992-deee8780-66ec-11e9-9991-eb85abb1d10a.png" width="80px" alt="dotnet bot in space" align ="right">
|| [**Basics**](#basics) • [**Contribution Guidelines**](#contribution) • [**Experiences**](#experiences) || [**Setup**](#setup) • [**Getting Started**](#getting-started) || [**Samples**](https://github.com/dotnet/try/tree/samples/Samples) ||

![Try_.NET Enabled](https://img.shields.io/badge/Try_.NET-Enabled-501078.svg)

[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/try/try-ci?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=495&branchName=master)

## Basics
**What is Try .NET**: Try .NET is an interactive documentation generator for .NET Core.

**Please Note**: At the moment Try .NET only works with C# documentation. 

## Contribution Guidelines
As we are still in the early stages of our development, we are unable to take any feature PRs at the moment but, we do intend to do this in the future.
Please feel free to file any bugs reports under our issues. And if you have any feature suggestion, please submit them under our issues using the community suggestions label.

## Experiences 
 Use Try .NET to create executable C# snippets for your websites or,  interactive markdown files that users can run on their machine. 

**Websites** 

Microsoft Docs uses Try .NET to create interactive documentation. Users can run and edit code al in the browser.
![Try NET_online](https://user-images.githubusercontent.com/2546640/57144765-c850cc00-6d8f-11e9-982d-50d2b6dc3591.gif)

**Interactive .NET documentation**

Try .NET enables .NET developers to create interactive markdown files.
To make your markdown files interactive, you will need to [.NET Core 3.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0), the dotnet try global tool(*coming soon*) and [Visual Studio](https://visualstudio.microsoft.com/) / [VS Code](https://code.visualstudio.com/)(or any other editor of your choice). 
![interactive_doc](https://user-images.githubusercontent.com/2546640/57158389-47a2c780-6db1-11e9-96ad-8c6e9ab52853.png)

## Setup
Before you get can start creating interactive documentation, you will need to install the following: 
- [.NET Core 3.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0) and [2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1) currently `dotnet try` global tool targets 2.1.
- [dotnet try global tool](https://www.nuget.org/packages/dotnet-try/)

`dotnet tool install --global dotnet-try --version 1.0.19264.11`

Updating to the latest version of the tool is easy just run the command below 

`dotnet tool update -g dotnet-try`

Once you have successfully installed `dotnet try` global tool, enter the command `dotnet try -h` you will see a list of commands:

| Command        | Purpose                                |
|----------------|----------------------------------------|
| `demo`         | Learn how to create Try .NET content with an interactive demo |
| `verify`       | Verify Markdown files in the target directory and its children.            |
## Getting Started

You can get started using either one of the options below. 

**Option1**: `dotnet try demo` 
- Create a new folder.
- `cd` to your new folder.
- Run command `dotnet try demo` 

**Option 2**: Starting from scratch.
1. Go to the terminal and create a folder called `mydoc`.
2. `cd` to the `mydoc` folder and create a new console app with the following command
 ```console
    > dotnet new console -o myApp
```
This will create a console app with the files `myApp.csproj` and `Program.cs`.

3. Open `mydoc`folder in Visual Studio Code. 

4. Create a file called `doc.md`. Inside that file, add some text and a code fence:

````markdown
# My Interactive Document:

```cs --source-file ./myApp/Program.cs --project ./myApp/myApp.csproj
```
````
5. Now, navigate back to the `mydoc` folder and run the following command:
```console
     > dotnet try
```
You have created your first C# interactive developer experience. You should now be able to run your console app and see the result in the browser. 

**Option 3**: Explore our [samples Branch](https://github.com/dotnet/try/tree/samples). 
- Clone [this](https://github.com/dotnet/try/tree/samples) repo(checkout the samples branch `git checkout samples`)
- Read our quick [setup guide](Samples/Setup.md). 
