# Getting started with dotnet try 
## Setup
Before you get can start creating interactive documentation, you will need to install the following: 
- [.NET Core 3.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0) 
- [dotnet try global tool](https://www.nuget.org/packages/dotnet-try/)

`dotnet tool install --global dotnet-try`

Updating to the latest version of the tool is easy just run the command below 

`dotnet tool update -g dotnet-try`

Once you have successfully installed `dotnet try` global tool, enter the command `dotnet try -h` you will see a list of commands:

| Command        | Purpose                                |
|----------------|----------------------------------------|
| `demo`         | Learn how to create Try .NET content with an interactive demo |
| `verify`       | Verify Markdown files in the target directory and its children.            |

## Installing preview builds from `master`

To install the latest preview build from master, first uninstall the existing version of the tool:

`dotnet tool uninstall -g dotnet-try`

Then install from the preview package feed:

`dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" dotnet-try`

## Getting Started

You can get started using either one of the options below. 

**Option1**: `dotnet try demo` 
- Create a new folder.
- `cd` to your new folder.
- Run command `dotnet try demo` : This will load our interactive dotnet try getting started tutorial. 
<img src = "https://user-images.githubusercontent.com/2546640/68031087-4c2d3780-fc91-11e9-803f-228116e6afa2.png" width ="80%">

The tutorials below work you through the following:
- Creating a new Try .NET project.
- Display interactive snippets using C# regions.
- Creating Sessions
- Verifying your projects: `dotnet try verify` a compiler for your documentation.
- Passing Arguments
- Using read only snippets

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

**Option 3**: Explore our [samples Repo](https://github.com/dotnet/try-samples). 
- Clone the [dotnet/try-samples](https://github.com/dotnet/try-samples) repo.
- Follow the quick steps listed [here](https://github.com/dotnet/try-samples#basics) to get started.

Return to [README.md](README.md)
