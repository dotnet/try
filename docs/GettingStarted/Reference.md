# Reference

- [Quick Start](./QuickStart.md)
- [Create a New Project](./NewProject.md)
- [Show snippets using regions](./Regions.md)
- [Create Sessions](./Sessions.md)
- [Verify your Project](./Verify.md)
- [Passing Arguments](./PassingArgs.md)
- [Using Read-only Snippets](./ReadOnlySnippets.md)
- **Reference**

### dotnet try

`dotnet try` is a .NET Core tool that allows you to create interactive samples for your users.

**List of available dotnet try commands.**

If you shut down this project and type the command `dotnet try -h` you will see a list of commands:

| Command        | Purpose                                |
|----------------|----------------------------------------|
| `demo`         | launches getting started documentation |
| `verify`       | compiler for documentation             |

### Code Fence Options

`dotnet try` extends Markdown using set of options that are added after langauage keyword in the code fence (*see below*).

| Option          | Purpose                                                                    |
|-----------------|----------------------------------------------------------------------------|
| `--source-file` | enables you to point to a specific file.                                   |
| `--project`     | enables you to point to a specific project.                                |
| `--region`      | lets you specify the block of code that you want to display in the editor. |
