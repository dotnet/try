# Declare nullable property

The following code shows the existing definition for the `Person` class, displayed in a **nullable enabled** context. You should see the warning in the constructor that takes `firstName` and `lastName` arguments, but does not set the `MiddleName` property.

On this page, you're going to modify the `Person` class in different ways to remove the warning. Farther down this page, you see the code that uses the `Person` class. Different changes to the `Person` class will generate different warnings in those later samples.

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/NullableReferences.cs --region Nullable_PersonDefinition --session nullable
```

Think about your design intent. Should `MiddleName` always be non-null? Or does it better reflect your design for `MiddleName` to be `null` for some people? You can either initialize the `MiddleName` property, or change `string` to `string?` in the declaration:

```csharp
public string? MiddleName { get; set; }
```

After you've made that change, look at the two code snippets that follow. Once the environment analyzes your code, you'll see warnings. Examine each and adjust the code to fix the warnings. Start with the `GetLengthOfMiddleName` method:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/NullableReferences.cs --region Nullable_GetLengthMethod --session nullable
```

If needed, fix the code that calls it:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/NullableReferences.cs --region Nullable_Usage --session nullable
```

One possible solution for `GetLengthOfName` would be the following code:

```csharp
    private static int GetLengthOfMiddleName(Person p)
    {
            string? middleName = p.MiddleName;
            return middleName?.Length ?? 0;
    }
```

You  can let the *may be null* type flow among declarations. You can use the `?.` operator to safely dereference a *may be null* variable, combined with the `??` operator to provide a default value.

Nullable reference types enable you to declare your intent around reference types: some should never be `null`, while for others `null` indicates a missing value. By declaring your intent, the compiler can help you enforce that intent.

#### Next: [Asynchronous streams &raquo;](./asynchronous-streams.md)  Previous: [Nullable reference types  &laquo;](./nullable-reference-types.md) Home: [Home](index.md)  
