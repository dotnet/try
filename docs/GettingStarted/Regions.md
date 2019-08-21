# Step-by-step tutorial: Show snippets using regions

- [Quick Start](./QuickStart.md)
- [Create a New Project](./NewProject.md)
- **Show snippets using regions**
- [Create Sessions](./Sessions.md)
- [Verify your Project](./Verify.md)
- [Passing Arguments](./PassingArgs.md)
- [Using Read-only Snippets](./ReadOnlySnippets.md)
- [Reference](./Reference.md)

Code documentation almost always features code snippets in isolation, like this:

```cs 
Console.WriteLine(DateTime.Now);
```

The `dotnet try` tool provides a way to do this while also making the code sample interactive:

```cs --source-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj --region run1
Console.WriteLine("Hello World!");
```

This is done using the `--region` option, which targets a [C# code region](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-region). Let's try this in your project.


1. Open `Program.cs` from your `MyDocProject` project.

2. Start a code region by placing `#region say_hello` on the line above `Console.WriteLine("Hello World!");`. Then, place ` #endregion` on the line after.

    Your `Program.cs` should look like this:

    ```cs
    using System;

    namespace HelloWorld
    {
        class Program
        {
            static void Main(string[] args)
            {
                #region say_hello
                Console.WriteLine("Hello World!");
                #endregion
            }
        }
    }
    ```

3. In `doc.md`, modify your code fence, appending the `--region` option.

    ````markdown
    # My code sample:

    ```cs --source-file ./MyConsoleApp/Program.cs --project ./MyConsoleApp/MyConsoleApp.csproj --region say_hello
    ```
    ````

4. When you refresh your browser, your Try .NET editor should now only show a single line of code: 

    ```cs
    Console.WriteLine("Hello World!");
    ```

**NEXT: [Define Sessions &raquo;](./Sessions.md)**
