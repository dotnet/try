# Indices and ranges

The previous examples show two design decisions that caused much debate:

- Ranges are *exclusive*, meaning the element at the last index is not in the range.
- The index `^0` is *the end* of the collection, not *the last element* in the collection.

You can explore several examples that highlight reasons for these decisions on this page.  Start by defining an array of elements, and three variables to use as indices into that array. All these samples are tied together. You can edit the values of `x`, `y` and `z`, or the expressions in the other windows, click the *indices-ranges* button and see the results.

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/IndicesAndRanges.cs --region IndicesAndRanges_CreateRange --session indices-ranges
```

The choice that `^0` matches *the end* means math using the `Length` property is reasonable:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/IndicesAndRanges.cs --region IndicesAndRanges_MathWithLength --session indices-ranges
```

Ranges are *exclusive*, making consecutive, disjoint sequences clear:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/IndicesAndRanges.cs --region IndicesAndRanges_Disjoint --session indices-ranges
```

The choice of end means removing the same number of elements from each end of a sequence is obvious:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/IndicesAndRanges.cs --region IndicesAndRanges_RemoveFromEnds --session indices-ranges
```

Incomplete ranges can assume `0` or `^0` for the missing index.

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/IndicesAndRanges.cs --region IndicesAndRanges_IncompleteRanges --session indices-ranges
```

#### Next: [Indices and ranges scenario &raquo;](./indices-and-ranges-scenario.md) Previous: [Indices and ranges  &laquo;](./nullable-fix-class.md) Home: [Home](index.md) 
