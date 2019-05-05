# Learning Methods and Collections

### Methods
A **method** is a block of code that implements some action. `ToUpper()` is a method you can invoke on a string, like the *name* variable. It will return the same string, converted to uppercase.
``` cs --region methods --source-file .\myapp\Program.cs --project .\myapp\myapp.csproj 
var name ="Rain";
Console.WriteLine($"Hello {name.ToUpper()}!");
```
### Collections
**Collections** hold multiple values of the same type.

Replace the *name* variable with a *names* variable that has a list of names. Then use a `foreach loop` to iterate over all the names and say hello to each person.

``` cs --region collections --source-file .\myapp\Program.cs --project .\myapp\myapp.csproj 
 var names = new List<string> { "Rain", "Sage", "Lee" };
 foreach (var name in names)
     {
          Console.WriteLine($"Hello {name.ToUpper()}!");
     }
```
#### Previous - [Strings & Variables &laquo;](./Strings.md) Home - [Home](../README.md) 