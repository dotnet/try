## My interactive documentation

This is how you can create an interactive documentation for your projects. In your code fences just add the `--source-file` and `--project` options to specify your content file and your project file as well as the region name if any. `dotnet try` tool will extract the source code from the specified file and execute the code. 

Just begin by typing `dotnet try` in the command prompt.

```cs --source-file ./Program.cs --project ./Microsoft.DotNet.Try.ProjectTemplate.Tutorial.csproj --region HelloWorld
```

Congrats you have run your first sample.

Lets see how we can use the code from another region

```cs --source-file ./Program.cs --project ./Microsoft.DotNet.Try.ProjectTemplate.Tutorial.csproj --region DateTime
```