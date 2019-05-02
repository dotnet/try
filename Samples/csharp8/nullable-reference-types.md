# Nullable reference types

C# 8/0 defines new syntax and static analysis rules so that you can declare your design intent regarding the nullability of any reference variable.  

Earlier, C# compiled all code in a **nullable oblivious** context. The compiler had no information about your design intent: Did you expect every variable to be initialized and assigned to non-null values? Or was `null` a valid value that you should check before dereferencing the variable? In a **nullable oblivious** context, the compiler does not issue any nullability warnings, and assumes you understand your design intent throughout your codebase.

Beginning with C# 8.0, the compiler defaults to a **nullable disabled** context. You can also opt in to a **nullable enabled** context. A **nullable disabled** context behaves the same as the earlier **nullable oblivious** behavior. You can opt in to a **nullable enabled** context. In a **nullable enabled** context, any variable of a reference type is a **non-nullable reference**. You can declare reference type variables with the `?` suffix to indicate that a variable's value may be null.

In a **nullable enabled** context, the compiler uses static analysis to enforce these rules: 

- A non-nullable reference must be initialized to a non-null value.
- A non-nullable reference cannot be assigned to a variable that may be null.
- A nullable reference may only be dereferenced safely when static analysis determines its value cannot be null.

There may be situations where you know a variable is not null, even though static analysis cannot prove it. For those situations, you can use the null forgiveness operator, `!`, to declare that the variable is not null.

The nullable enabled context provides more safety against null reference exceptions. You can declare your intent, and the compiler issues warnings when your code isn't consistent with that declared intent.

## Explore a basic scenario

Try running the following code block:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/NullableReferences.cs --region Nullable_Usage
```

**Note:** That block throws a `NullReferenceException` because the `MiddleName` property is null for this person. Someone coded this error and the compiler didn't provide any helpful information to inform them that this code wasn't safe. In the next pages, you'll explore how nullable reference types would have avoided this bug.

#### Next: [Declare design intent &raquo;](./nullable-fix-class.md)     Previous: [Using declarations  &laquo;](./using-declarations-ref-structs.md)    Home: [Home](index.md)  
