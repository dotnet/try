# Step-by-step tutorial: Verify your project

- [Quick Start](./QuickStart.md)
- [Create a New Project](./NewProject.md)
- [Show snippets using regions](./Regions.md)
- [Create Sessions](./Sessions.md)
- **Verify your Project**
- [Passing Arguments](./PassingArgs.md)
- [Using Read-only Snippets](./ReadOnlySnippets.md)
- [Reference](./Reference.md)

`dotnet try verify` is a compiler for your documentation. With this command, you can make sure that every code snippet will work and is in sync with the backing project. The goal of `dotnet try verify` is to enable you to check the correctness of your documentation as you work, and to enable the same checks inside of your build pipeline.

In the `doc.md` file in your `MyDocProject`, change the `--project` option in one of the code fences to a nonexistent file name, like `./nonexistent.csproj`. Then refresh your browser.

Where the code editor was, you will now see a warning (_"No project or package specified"_). This is a clear way to indicate that your Markdown document is incorrectly configured.

You can see this same error in your terminal by running the `dotnet try verify` command. You can run this command in the `MyDocProject` folder, or from elsewhere using `dotnet try verify <path-to-folder>`. It should look similar to this: 

![dotnet verify -errorproject](https://user-images.githubusercontent.com/2546640/53291265-8f3c2000-377e-11e9-9b82-b7ea3ce1ab05.PNG)

Try making other changes to the code fence options. Mistype an option name, specify a nonexistent code region, or make a non-compiling change in your backing project. You'll see different errors pointing out the various problems.

When `dotnet try verify` detects errors, it will return a non-zero exit code. When everything looks good, it returns `0`. You can use this in your continuous integration scripts to prevent code changes from breaking your documentation.

**NEXT: [Passing Arguments &raquo;](./PassingArgs.md)**
