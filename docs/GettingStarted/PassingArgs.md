# Passing arguments to your sample project

- [Quick Start](./QuickStart.md)
- [Create a New Project](./NewProject.md)
- [Show snippets using regions](./Regions.md)
- [Create Sessions](./Sessions.md)
- [Verify your Project](./Verify.md)
- **Passing Arguments**
- [Using Read-only Snippets](./ReadOnlySnippets.md)
- [Reference](./Reference.md)

Now that you have a few different snippets on your page, you probably want to call the code in the snippet corresponding to the run button that was clicked. Remember, `dotnet try` will replace some code and then invoke your program's `Main` method. But you may not want to run all of the code in your program for every button. If you can switch code paths based on which button was clicked, you can get a lot more use out of that sample project.

You may have noticed that the signature of `Program.Main` in the [QuickStart](./QuickStart.md)'s backing project (`Snippets.csproj`) looks a little strange:

```cs --editable false --region Main --source-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj
```

Instead of the familiar `Main(string[] args)` entry point, this program's entry point uses the new [experimental library](https://github.com/dotnet/command-line-api/wiki/DragonFruit-overview) `System.CommandLine.DragonFruit` to parse the arguments that were specified in your Markdown file's code fence. The `QuickStart.md` sample uses these arguments to route to different methods, but you can probably think of other ways to use these arguments. As you saw from the tutorial, you're not required to use any particular library in your backing project. But the command line arguments are available if you want to respond to them, and `DragonFruit` is a concise option for doing so.

_Congratulations! You've finished the `dotnet try` step-by-step tutorial._

**NEXT: [Using Read-only Snippets &raquo;](./ReadOnlySnippets.md)**
