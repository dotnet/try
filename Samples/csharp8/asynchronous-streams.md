## Asynchronous streams

Starting with C# 8.0, you can create and consume streams asynchronously. A method that returns an asynchronous stream has three properties:

1. It's declared with the `async` modifier.
1. It returns an `System.Collections.Generic.IAsyncEnumerable<T>`
1. The method contains `yield return` statements to return successive elements in the asynchronous stream.

Consuming an asynchronous stream requires you to add the `await` keyword before the `foreach` keyword when you enumerate the elements of the stream. Adding the `await` keyword requires the method that enumerates the asynchronous stream to be declared with the `async` modifier and to return a type allowed for an `async` method. Typically that means returning a `System.Threading.Tasks.Task` or `System.Threading.Tasks.Task<T>`. It can also be a `System.Threading.Tasks.ValueTask` or `System.Threading.Tasks.ValueTask<T>`. A method can both consume and produce an asynchronous stream, which means it would return an `System.Collections.Generic.IAsyncEnumerable<T>`. The following code generates a sequence from 0 to 19. Every 3 elements, it pauses for 2 seconds to simulate retrieving the next set from a device:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/AsyncStreams.cs --region AsyncStreams_Declare --session async-stream
```

You would enumerate the sequence using the `await foreach` statement:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/AsyncStreams.cs --region AsyncStreams_Consume --session async-stream
```

Click the run button to experiment. Look at the times when elements are retrieved. Change some of the constants to experiment with different values, or time delays. 

> *Note*:
> This sandbox environment will timeout and halt programs that appear to be stuck. Avoid very long delays or very large sequences.

#### Next: [Indices and ranges &raquo;](./indices-and-ranges.md)        Previous: [Nullable reference types  &laquo;](./nullable-fix-class.md) Home: [Home](index.md)  
