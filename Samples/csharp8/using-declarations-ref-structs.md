# using declarations

A **using declaration** is a variable declaration preceded by the `using` keyword. It tells the compiler that the variable being declared should be disposed at the end of the enclosing scope. The `using` block has always been recommended for local variables when the type implements `IDisposable`. The following example shows a hypothetical use of two objects that implement `IDisposable`. Run the code and you'll see the `ResourceHog` writes a message when it is being disposed:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/UsingDeclarationsRefStruct.cs --region Using_Block
```

In the preceding example, each object is disposed when the closing brace for its `using` statement is reached. The new `using` declaration generates code that automatically disposes an object at the end of the enclosing block. That results in cleaner code that is easier to understand:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/UsingDeclarationsRefStruct.cs --region Using_Declaration
```

In the preceding example, the resources are disposed at the end of the block. Run it to see the results, which should model the previous sample.

In both cases, the compiler generates the call to `Dispose()`. The compiler generates an error if the expression in the using statement is not disposable.

# Disposable ref structs

A `struct` declared with the `ref` modifier may not implement any interfaces and so cannot implement `System.IDisposable`. Therefore, to enable a `ref struct` to be disposed, it must have an accessible `void Dispose()` method. This also applies to `readonly ref struct` declarations. These types can now release resources in `using` declarations or `using` blocks.

#### Next: [nullable reference types &raquo;](./nullable-reference-types.md)    Previous: [Static local functions  &laquo;](./static-local-functions.md)    Home: [Home](index.md)  
