# Step-by-step tutorial: Define Sessions

- [Quick Start](./QuickStart.md)
- [Create a New Project](./NewProject.md)
- [Define Regions](./Regions.md)
- **Create Sessions**
- [Verify your Project](./Verify.md)
- [Passing Arguments](./PassingArgs.md)
- [Using Read-only Snippets](./ReadOnlySnippets.md)
- [Glossary](./Glossary.md)

Let's make this a little more interesting. Your project probably has more than one snippet you want people to be able to run. Sessions allow you to run these code snippets independently of one another.

1. In your `MyConsoleApp` folder, add a new file called `Cat.cs` and add the following code:

    ```cs
    using System;

    namespace MyConsoleApp
    {
        class Cat
        {
            #region what_the_cat_says
            public string Say() 
            {
                return "meow!";
            }
            #endregion
        }
    }
    ```

2. Update your `Program.cs`, replacing the contents of the `hello` region with this:

    ```cs
    Console.WriteLine(new Cat().Say());
    ```

3. In `doc.md`, create two separate snippets with one pointing to `Program.cs` and the `say_hello` region and the other pointing to `Cat.cs` and the `what_the_cat_says` region. 

    ````markdown
    # My code sample:
    ```cs --source-file .\MyConsoleApp\Program.cs --project .\MyConsoleApp\MyConsoleApp.csproj --region say_hello
    ```
    ```cs --source-file .\MyConsoleApp\Cat.cs --project .\MyConsoleApp\MyConsoleApp.csproj --region what_the_cat_says
    ```
    ````
    
    Once you've made these changes, refresh the page and hit `Run`.

    These code snippets compile and execute together. You can see this by editing the method name in the second editor. After a moment, you'll see a red squiggle indicating a compile error:

    ![image](https://user-images.githubusercontent.com/547415/53462150-afc2df00-39f7-11e9-8a22-7ed5b2825cb6.png)

    But you might want these snippets to be able to compile and execute independently of one another. That way, if your user breaks the code in one, the others will still work. To do this, you can use a `--session` option, providing a new session name for each snippet. Any snippets that share a session name will compile together.

4. Go back to the code fence snippets we created in step 3 and add a `--session` option to each one:

    ````markdown
     # My code sample:
    ```cs --source-file .\MyConsoleApp\Program.cs --project .\MyConsoleApp\MyConsoleApp.csproj --region say_hello --session one
    ```
    ```cs --source-file .\MyConsoleApp\Cat.cs --project .\MyConsoleApp\MyConsoleApp.csproj --region what_the_cat_says --session two 
    ```
    ````

    Once you've made this change, refresh the page. You should see two separate output panels and two run buttons containing the text that you specified for the session names.

    In the second editor, change `"meow"` to `"purrrr"`. Click `one` and then click `two`.

Well done! Now, you have a project that you are almost ready to share with others. A big part of good documentation is making sure everything works. In `dotnet try` we do this by using `dotnet try verify` which we will explore in the next module.

**NEXT: [Verify your Project &raquo;](./Verify.md)**
