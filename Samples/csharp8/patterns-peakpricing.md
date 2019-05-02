# Add peak pricing

For the final feature, the toll authority wants to add time sensitive peak pricing. During the morning and evening rush hours, the tolls are doubled. That rule only affects traffic in one direction: inbound to the city in the morning, and outbound in the evening rush hour. During other times during the workday, tolls increase by 50%. Late night and early morning, tolls are reduced by 25%. During the weekend, it's the normal rate, regardless of the time.

You'll use pattern matching for this feature, but you'll integrate it with other techniques. You could build a single pattern match expression that would account for all the combinations of direction, day of the week, and time. The result would be a complicated expression. It would be hard to read and difficult to understand. That makes it hard to ensure correctness. Instead, combine those methods to build a tuple of values that concisely describes all those states. Then use pattern matching to calculate a multiplier for the toll. The tuple contains three discrete conditions:

- The day is either a weekday or a weekend.
- The band of time when the toll is collected.
- The direction is into the city or out of the city

The following table shows the combinations of input values and the peak pricing multiplier:

| Day        | Time         | Direction | Premium |
| ---------- | ------------ | --------- |--------:|
| Weekday    | morning rush | inbound   | x 2.00  |
| Weekday    | morning rush | outbound  | x 1.00  |
| Weekday    | daytime      | inbound   | x 1.50  |
| Weekday    | daytime      | outbound  | x 1.50  |
| Weekday    | evening rush | inbound   | x 1.00  |
| Weekday    | evening rush | outbound  | x 2.00  |
| Weekday    | overnight    | inbound   | x 0.75  |
| Weekday    | overnight    | outbound  | x 0.75  |
| Weekend    | morning rush | inbound   | x 1.00  |
| Weekend    | morning rush | outbound  | x 1.00  |
| Weekend    | daytime      | inbound   | x 1.00  |
| Weekend    | daytime      | outbound  | x 1.00  |
| Weekend    | evening rush | inbound   | x 1.00  |
| Weekend    | evening rush | outbound  | x 1.00  |
| Weekend    | overnight    | inbound   | x 1.00  |
| Weekend    | overnight    | outbound  | x 1.00  |

There are 16 different combinations of the three variables. By combining some of the conditions, you'll simplify the final switch expression.

The system that collects the tolls uses a <xref:System.DateTime> structure for the time when the toll was collected. Build member methods that create the variables from the preceding table. The following function uses a pattern matching switch expression to express whether a <xref:System.DateTime> represents a weekend or a weekday:

```csharp
private static bool IsWeekDay(DateTime timeOfToll) =>
    timeOfToll.DayOfWeek switch
    {
        DayOfWeek.Monday    => true,
        DayOfWeek.Tuesday   => true,
        DayOfWeek.Wednesday => true,
        DayOfWeek.Thursday  => true,
        DayOfWeek.Friday    => true,
        DayOfWeek.Saturday  => false,
        DayOfWeek.Sunday    => false
    };
```

That method works, but it's repetitious. You can simplify it, as shown in the following code:

```csharp
private static bool IsWeekDay(DateTime timeOfToll) =>
    timeOfToll.DayOfWeek switch
    {
        DayOfWeek.Saturday => false,
        DayOfWeek.Sunday   => false,
        _                  => true
    };
```

Next, add a similar function to categorize the time into the blocks:

```csharp
private enum TimeBand
{
    MorningRush,
    Daytime,
    EveningRush,
    Overnight
}

private static GetTimeBand GetTimeBand(DateTime timeOfToll)
{
    int hour = timeOfToll.Hour;
    if (hour < 6)
        return TimeBand.Overnight;
    else if (hour < 10)
        return TimeBand.MorningRush;
    else if (hour < 16)
        return TimeBand.Daytime;
    else if (hour < 20)
        return TimeBand.EveningRush;
    else
        return TimeBand.Overnight;
}
```

The previous method doesn't use pattern matching. It's clearer using a familiar cascade of `if` statements. You do add a private `enum` to convert each range of time to a discrete value.

After you create those methods, you can use another `switch` expression with the **tuple pattern** to calculate the pricing premium. You could build a `switch` expression with all 16 arms:

```csharp
public decimal PeakTimePremiumFull(DateTime timeOfToll, bool inbound) =>
    (IsWeekDay(timeOfToll), GetTimeBand(timeOfToll), inbound) switch
    {
        (true,  TimeBand.MorningRush, true)  => 2.00m,
        (true,  TimeBand.MorningRush, false) => 1.00m,
        (true,  TimeBand.Daytime,     true)  => 1.50m,
        (true,  TimeBand.Daytime,     false) => 1.50m,
        (true,  TimeBand.EveningRush, true)  => 1.00m,
        (true,  TimeBand.EveningRush, false) => 2.00m,
        (true,  TimeBand.Overnight,   true)  => 0.75m,
        (true,  TimeBand.Overnight,   false) => 0.75m,
        (false, TimeBand.MorningRush, true)  => 1.00m,
        (false, TimeBand.MorningRush, false) => 1.00m,
        (false, TimeBand.Daytime,     true)  => 1.00m,
        (false, TimeBand.Daytime,     false) => 1.00m,
        (false, TimeBand.EveningRush, true)  => 1.00m,
        (false, TimeBand.EveningRush, false) => 1.00m,
        (false, TimeBand.Overnight,   true)  => 1.00m,
        (false, TimeBand.Overnight,   false) => 1.00m,
    };
```

The above code works, but it can be simplified. All eight combinations for the weekend have the same toll. You can replace all eight with the following line:

```csharp
(false, _, _) => 1.0m,
```

Both inbound and outbound traffic have the same multiplier during the weekday daytime and overnight hours. Those four switch arms can be replaced with the following two lines:

```csharp
(true, TimeBand.Overnight, _) => 0.75m,
(true, TimeBand.Daytime, _)   => 1.5m,
```

The code should look like the following code after those two changes:

```csharp
public decimal PeakTimePremium(DateTime timeOfToll, bool inbound) =>
    (IsWeekDay(timeOfToll), GetTimeBand(timeOfToll), inbound) switch
    {
        (true, TimeBand.MorningRush, true)  => 2.00m,
        (true, TimeBand.MorningRush, false) => 1.00m,
        (true, TimeBand.Daytime,     _)     => 1.50m,
        (true, TimeBand.EveningRush, true)  => 1.00m,
        (true, TimeBand.EveningRush, false) => 2.00m,
        (true, TimeBand.Overnight,   _)     => 0.75m,
        (false, _,                   _)     => 1.00m,
    };
```

Finally, you can remove the two rush hour times that pay the regular price. Once you remove those arms, you can replace the `false` with a discard (`_`) in the final switch arm. You'll have the following finished method:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/Patterns.cs --region Pattern_PeakTime
```

This example highlights one of the advantages of pattern matching: the pattern branches are evaluated in order. If you rearrange them so that an earlier branch handles one of your later cases, the compiler warns you about the unreachable code. Those language rules made it easier to do the preceding simplifications with confidence that the code didn't change.

Pattern matching makes some types of code more readable and offers an alternative to object-oriented techniques when you can't add code to your classes. The cloud is causing data and functionality to live apart. The *shape* of the data and the *operations* on it aren't necessarily described together. In this tutorial, you consumed existing data in entirely different ways from its original function. Pattern matching gave you the ability to write functionality that overrode those types, even though you couldn't extend them.

#### Next: [static local functions &raquo;](./static-local-functions.md)    Previous: [Add occupancy pricing  &laquo;](./patterns-occupancy.md) Home: [Home](index.md)  
