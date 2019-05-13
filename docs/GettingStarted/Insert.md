# Read-only code snippets

```cs  --editable false --region usings --destination-file ./Snippets/Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
```

```cs --hidden --editable false
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

```cs --editable false  --region custom_code
public IEnumerable<string> PrintMe(){
    yield return "What an adventure";
}
```

```cs --editable false --hidden
public class HiddenObject
{
}
```

```cs --editable false --hidden --destination-file ./Snippets/Program.cs
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

```cs --editable false --hidden --destination-file ./Snippets/Program.cs
    public class ProgramUtility
    {
    }
}
```

the following code will run before the editable part

```cs --editable false --destination-file ./Snippets/Program.cs --region run1
Console.WriteLine($"printed before the region execution: counter value is {counter}");
```

edit the code here, try manipulating the value on the variable `counter`

```csharp --source-file ./Snippets/Program.cs --region run1
```

and the following code will run after the editable part

```cs --editable false --destination-file ./Snippets/Program.cs --region run1 
Console.WriteLine($"printed after the region execution: counter value is {counter}");
```


