# Creating interactive documentation with Try .NET

## Setup
Before getting started with the sample above, you need to install the `dotnet try` global tool. In a terminal execute

```console
dotnet tool install --global dotnet-try
```

Once installed you can execute 
```console
dotnet try -h
```
and see the list of available commands.

## Executing your first sample

In the directory of the project execute 
```console
dotnet try
```
This should launch a browser window. The following code fence will be replaced by the editor and you can execute the code by hitting the run button.

```cs --source-file ./Program.cs --project ./Microsoft.DotNet.Try.ProjectTemplate.Tutorial.csproj --region HelloWorld
```

## What's happening behind the scenes

Code fences are a standard way to include code in your markdown files. The only change you need to do is to add few options in the first line of your code snippet. If you notice the above code snippet, there are three options in action.

| Option                                 | What it does                                                                                                                |
|----------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `--project ./Microsoft.DotNet.Try.ProjectTemplate.Tutorial.csproj` | Points to the project that the sample is part of. (Optional. Defaults to any .csproj in the same folder as the `.md` file.) |
| `--region HelloWorld`                        | Identifes a C# code `#region` to focus on. (Optional. If not specified, the whole file is displayed in the editor.)         |
| `--source-file ./Program.cs`  | Points to the file where the sample code is pulled from.  

If you navigate back to Program.cs you will be able to see the various regions and the context in which your code is being execute. As an exercise, try to change the region in the previous code snippet to `DateTime`  and then refresh the browser. You should be able to see the text that is a part of the `DateTime` region now.

## Learn More

The above are the basic to get you started with creating your own interactive documentation. To learn more about the `dotnet try` features, at the command line execute

```console
dotnet try demo
```

This interactive demo will walk you through the various features in `dotnet try`.

## Feedback

We love to hear from you. If you have any suggestions or feedback please reach out to us on [GitHub](https://github.com/dotnet/try)
