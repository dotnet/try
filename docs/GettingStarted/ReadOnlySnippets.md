# Step-by-step tutorial: Read-only code snippets

- [Quick Start](./QuickStart.md)
- [Create a New Project](./NewProject.md)
- [Show snippets using regions](./Regions.md)
- [Create Sessions](./Sessions.md)
- [Verify your Project](./Verify.md)
- [Passing Arguments](./PassingArgs.md)
- **Using Read-only Snippets**
- [Reference](./Reference.md)


```cs  --editable false --region usings --destination-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
```

```cs --hidden --editable false --project ./Snippets/Snippets.csproj
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class DataObject
{
     #region custom_code
     #endregion
}
```

Declare a method that can be used from any instance of `DataObject` class, which implementation details are omitted.

```cs --editable false  --region custom_code --project ./Snippets/Snippets.csproj
public IEnumerable<string> PrintMe(){
    yield return "What an adventure";
}
```

```cs --editable false --hidden --project ./Snippets/Snippets.csproj
public class HiddenObject
{
}
```

```cs --editable false --hidden --destination-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj
 #region usings
 #endregion

namespace Snippets
{
    public class Program
    {
        static void Main()
        {
            var counter = 0;
            #region run1
            Console.WriteLine(DateTime.Now);
            #endregion

            #region run2
            Console.WriteLine(DateTime.Now);
            #endregion

            Console.WriteLine("this is from hidden include");
        }
    }

```

```cs --editable false --hidden --destination-file ./Snippets/Program.cs --project ./Snippets/Snippets.csproj
    public class ProgramUtility
    {
    }
}
```

the following code will run before the editable part

```cs --editable false --destination-file ./Snippets/Program.cs --region run1 --project ./Snippets/Snippets.csproj
Console.WriteLine($"printed before the region execution: counter value is {counter}");
```

edit the code here, try manipulating the value on the variable `counter`

```csharp --source-file ./Snippets/Program.cs --region run1 --project ./Snippets/Snippets.csproj
```

and the following code will run after the editable part

```cs --editable false --destination-file ./Snippets/Program.cs --region run1 --project ./Snippets/Snippets.csproj
Console.WriteLine($"printed after the region execution: counter value is {counter}");
```

**NEXT: [Reference &raquo;](./Reference.md)**


