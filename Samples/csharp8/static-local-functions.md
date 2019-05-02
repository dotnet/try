# Static local functions

You can now add the `static` modifier to local functions to ensure that local function doesn't capture (reference) any variables from the enclosing scope. Doing so generates `CS8421`, "A static local function can't contain a reference to \<variable>." 

Typically, any resources declared or used in a function are released when the function exits. However, when a function *captures* a variable that is declared in an enclosing scope, that resource has a lifetime that extends beyond the function itself. In many scenarios, this isn't an issue. In others, capturing and preventing the release of resources can impact performance. Declaring local functions as `static` ensures this doesn't happen.

Consider the following code. The local function `LocalFunction` accesses the variable `y`, declared in the enclosing scope (the method `M`). Therefore, `LocalFunction` can't be declared with the `static` modifier:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/StaticLocalFunctions.cs --region LocalFunction_Counting
```

The local iterator method *captures* the parameters `start` and `end`. Add the `static` modifier to see the compiler generated warning. You'll need to declare arguments to the local function so that those values aren't captured. Make the changes shown below to get the warning removed:

```csharp
static IEnumerable<int> localCounter(int first, int endLocation)
{
    for (int i = first; i < endLocation; i++)
        yield return i;
}
```

The sample should compile and run correctly.

#### Next: [disposable ref structs and using declarations &raquo;](using-declarations-ref-structs.md)    Previous: [Add peak pricing  &laquo;](./patterns-peakpricing.md)    Home: [Home](index.md)
