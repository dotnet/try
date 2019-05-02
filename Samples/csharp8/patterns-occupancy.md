# Add occupancy pricing

The toll authority wants to encourage vehicles to travel at maximum capacity. They've decided to charge more when vehicles have fewer passengers, and encourage full vehicles by offering lower pricing:

- Cars and taxis with no passengers pay an extra $0.50.
- Cars and taxis with two passengers get a 0.50 discount.
- Cars and taxis with three or more passengers get a $1.00 discount.
- Buses that are less than 50% full pay an extra $2.00.
- Buses that are more than 90% full get a $1.00 discount.

These rules can be implemented using the **property pattern** in the same switch expression. The property pattern examines properties of the object once the type has been determined. The single case for a `Car` expands to four different cases, as does the single case for `Taxi`:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/Patterns.cs --region Pattern_CarTaxiOccupancy
```

The first three cases test the type as a `Car`, then check the value of the `Passengers` property. If both match, that expression is evaluated and returned. Next, implement the occupancy rules by expanding the cases for buses, as shown in the following example:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/Patterns.cs --region Pattern_BusOccupancy
```

The toll authority isn't concerned with the number of passengers in the delivery trucks. Instead, they charge more based on the weight class of the trucks. Trucks over 5000 lbs are charged an extra $5.00. Light trucks under 3000 lbs are given a $2.00 discount. That rule is implemented with the following code:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/Patterns.cs --region Pattern_DeliveryTruckWeight
```

Many of these switch arms are examples of **recursive patterns**. For example, `Car { Passengers: 1}` shows a constant pattern inside a property pattern.

You can make this code less repetitive by using nested switches. The `Car` and `Taxi` both have four different arms in the preceding examples. In both cases, you can create a type pattern that feeds into a property pattern. This technique is shown in the following code:

```cs --project ./ExploreCsharpEight/ExploreCsharpEight.csproj --source-file ./ExploreCsharpEight/Patterns.cs --region Pattern_ChainedPatterns
```

In the preceding sample, using a recursive expression means you don't repeat the `Car` and `Taxi` arms containing child arms that test the property value. This technique isn't used for the `Bus` and `DeliveryTruck` arms because those arms are testing ranges for the property, not discrete values.

#### Next: [Add peak time pricing &raquo;](./patterns-peakpricing.md)   Previous: [Type pattern  &laquo;](./patterns-types.md) Home: [Home](index.md)
