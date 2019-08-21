# Quick Start

- **Quick Start**
- [Create a New Project](./NewProject.md)
- [Show snippets using regions](./Regions.md)
- [Create Sessions](./Sessions.md)
- [Verify your Project](./Verify.md)
- [Passing Arguments](./PassingArgs.md)
- [Using Read-only Snippets](./ReadOnlySnippets.md)
- [Reference](./Reference.md)

Congratulations! You just ran `dotnet try demo`. This is an interactive guide to get you familiar with the `dotnet try` tool. 

### What is dotnet try?

`dotnet try` is a tool that allows you to create interactive documentation. Try editing the code in the editor below and then clicking the `Run example 1` button.

#### Example 1

```csharp --source-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj --region run1
```

### What's happening behind the scenes?

The content for this page is in a Markdown file, `QuickStart.md`, which you can find in the folder where you just ran `dotnet try demo`. The sample code that you see in the editor is in the Snippets subfolder, in a regular C# project that was created using `dotnet new console`.

For reference, the path to the demo folder is in the upper right-hand corner of the page. Feel free to edit the demo files. You can always recreate them by running `dotnet try demo` again in a fresh folder.

### Code fence options

The term "code fence" refers to the Markdown delimiters around a multi-line block of code. Here's an example:

````markdown
```cs 
Console.WriteLine("Hello World!");
```
````

The `dotnet try` tool extends Markdown using a set of options that can be added after the language keyword in the code fence. This lets you reference sample code from the backing project, allowing a normal C# project, rather than the documentation, to be the source of truth. This removes the need to copy and paste code snippets from a code sample into your Markdown file.

#### Example 2

```cs --source-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj --region run2  
```

For example, the code snippet above was extended using `dotnet try`. The code fence that wires it up looks like this: 

````markdown
```cs --source-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj --region run2 
```
````

### What do the options do?

| Option                                 | What it does                                                                                                                |
|----------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| `--project ./Snippets/Snippets.csproj` | Points to the project that the sample is part of. (Optional. Defaults to any .csproj in the same folder as the `.md` file.) |
| `--region run2`                        | Identifes a C# code `#region` to focus on. (Optional. If not specified, the whole file is displayed in the editor.)         |
| `--source-file ./Snippets/Program.cs`  | Points to the file where the sample code is pulled from.                                                                    |

### Document Verification

Verifying that your code samples work is vital to your user experience, so `dotnet try` acts as a compiler for your documentation. In a text editor, open `QuickStart.md` from your demo folder and change the `--region` option in **Example 1** from `--region run1` to `--region run5`. This change will break the sample. You can see this error in two different ways.

1. Refresh the browser. You'll now see an error like this:

    ![image](https://user-images.githubusercontent.com/547415/53391389-14743000-394b-11e9-8305-1f2a3b72f95a.png)


2. Since it's also important to be able to verify your documentation using automation so that broken code doesn't get checked in, we added the `dotnet try verify` command. At the command line, navigate to the root of your demo folder and run `dotnet try verify`. You will see something similar to this:

    ![dotnet verify -error](https://user-images.githubusercontent.com/2546640/53290283-c8b95f00-376f-11e9-8350-1a3e470267b5.PNG)

Now change the region option back to `--region run1`. Save the changes. If you re-run the  `dotnet try verify` you'll see all green check marks and the error is gone. 

### Exercise

Here's a quick exercise that will teach you how to create a new snippet, use a region in an existing backing project. 

1. In a text editor, open `./Snippets/Program.cs` under your demo folder.

2. Find the `Run3` method. It looks like this:

```cs
public static void Run3()
{
    #region run3
    #endregion
}
```

3. Add the code below inside the `run3` region.

```cs
var primes = String.Format("Prime numbers less than 10: {0}, {1}, {2}, {3}", 2, 3, 5, 7);
Console.WriteLine(primes);
```

3. Update this markdown file (`QuickStart.md`) and add a new code fence that references the code in the `run3` region inside the `Program.Run3` method. 

***Add your code fence here.***

4. Refresh the browser.

*Hint* Look at the static code snippet above, under **Code fence options**. Make sure to update `--region` option.

**NEXT: [Create a New Project &raquo;](./NewProject.md)**
