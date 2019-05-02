using System;
using System.Collections.Generic;
using System.Text;
using CommercialRegistration;
using ConsumerVehicleRegistration;
using LiveryRegistration;


namespace ExploreCsharpEight
{
    // I made this interface so I can usethe same test routine
    // with each iteration of the toll calculator
    interface ITollCalculator
    {
        decimal CalculateToll(object vehicle);
    }
    
    class TollCalculator_V1 : ITollCalculator
    {
        #region Pattern_CalculateToll
        public decimal CalculateToll(object vehicle) =>
            vehicle switch
            {
                Car c => 2.00m,
                Taxi t => 3.50m,
                Bus b => 5.00m,
                DeliveryTruck t => 10.00m,
                {} => throw new ArgumentException(message: "Not a known vehicle type", paramName: nameof(vehicle)),
                null => throw new ArgumentNullException(nameof(vehicle))
            };
        #endregion
    }

    class TollCalculator_V2 : ITollCalculator
    {
        #region Pattern_CarTaxiOccupancy
        public decimal CalculateToll(object vehicle) =>
            vehicle switch
            {
                Car { Passengers: 0 } => 2.00m + 0.50m,
                Car { Passengers: 1 } => 2.0m,
                Car { Passengers: 2 } => 2.0m - 0.50m,
                Car _ => 2.00m - 1.0m,

                Taxi { Fares: 0 } => 3.50m + 1.00m,
                Taxi { Fares: 1 } => 3.50m,
                Taxi { Fares: 2 } => 3.50m - 0.50m,
                Taxi _ => 3.50m - 1.00m,

                Bus b => 5.00m,
                DeliveryTruck t => 10.00m,
                {} => throw new ArgumentException(message: "Not a known vehicle type", paramName: nameof(vehicle)),
                null => throw new ArgumentNullException(nameof(vehicle))
            };
        #endregion
    }

    class TollCalculator_V3 : ITollCalculator
    {
        #region Pattern_BusOccupancy
        public decimal CalculateToll(object vehicle) =>
            vehicle switch
            {
                Car { Passengers: 0 } => 2.00m + 0.50m,
                Car { Passengers: 1 } => 2.0m,
                Car { Passengers: 2 } => 2.0m - 0.50m,
                Car _ => 2.00m - 1.0m,

                Taxi { Fares: 0 } => 3.50m + 1.00m,
                Taxi { Fares: 1 } => 3.50m,
                Taxi { Fares: 2 } => 3.50m - 0.50m,
                Taxi _ => 3.50m - 1.00m,

                Bus b when ((double)b.Riders / (double)b.Capacity) < 0.50 => 5.00m + 2.00m,
                Bus b when ((double)b.Riders / (double)b.Capacity) > 0.90 => 5.00m - 1.00m,
                Bus _ => 5.00m,

                DeliveryTruck t => 10.00m,
                {} => throw new ArgumentException(message: "Not a known vehicle type", paramName: nameof(vehicle)),
                null => throw new ArgumentNullException(nameof(vehicle))
            };
        #endregion
    }

    class TollCalculator_V4 : ITollCalculator
    {
        #region Pattern_DeliveryTruckWeight
        public decimal CalculateToll(object vehicle) =>
            vehicle switch
            {
                Car { Passengers: 0 } => 2.00m + 0.50m,
                Car { Passengers: 1 } => 2.0m,
                Car { Passengers: 2 } => 2.0m - 0.50m,
                Car _ => 2.00m - 1.0m,

                Taxi { Fares: 0 } => 3.50m + 1.00m,
                Taxi { Fares: 1 } => 3.50m,
                Taxi { Fares: 2 } => 3.50m - 0.50m,
                Taxi _ => 3.50m - 1.00m,

                Bus b when ((double)b.Riders / (double)b.Capacity) < 0.50 => 5.00m + 2.00m,
                Bus b when ((double)b.Riders / (double)b.Capacity) > 0.90 => 5.00m - 1.00m,
                Bus _ => 5.00m,

                DeliveryTruck t when (t.GrossWeightClass > 5000) => 10.00m + 5.00m,
                DeliveryTruck t when (t.GrossWeightClass < 3000) => 10.00m - 2.00m,
                DeliveryTruck _ => 10.00m,

                {} => throw new ArgumentException(message: "Not a known vehicle type", paramName: nameof(vehicle)),
                null => throw new ArgumentNullException(nameof(vehicle))
            };
        #endregion
    }

    class TollCalculator_V5 : ITollCalculator
    {
        #region Pattern_ChainedPatterns
        public decimal CalculateToll(object vehicle) =>
            vehicle switch
            {
                Car c => c.Passengers switch
                {
                    0 => 2.00m + 0.5m,
                    1 => 2.0m,
                    2 => 2.0m - 0.5m,
                    _ => 2.00m - 1.0m
                },

                Taxi t => t.Fares switch
                {
                    0 => 3.50m + 1.00m,
                    1 => 3.50m,
                    2 => 3.50m - 0.50m,
                    _ => 3.50m - 1.00m
                },

                Bus b when ((double)b.Riders / (double)b.Capacity) < 0.50 => 5.00m + 2.00m,
                Bus b when ((double)b.Riders / (double)b.Capacity) > 0.90 => 5.00m - 1.00m,
                Bus b => 5.00m,

                DeliveryTruck t when (t.GrossWeightClass > 5000) => 10.00m + 5.00m,
                DeliveryTruck t when (t.GrossWeightClass < 3000) => 10.00m - 2.00m,
                DeliveryTruck t => 10.00m,

                {} => throw new ArgumentException(message: "Not a known vehicle type", paramName: nameof(vehicle)),
                null => throw new ArgumentNullException(nameof(vehicle))
            };
        #endregion

        private enum TimeBand
        {
            MorningRush,
            Daytime,
            EveningRush,
            Overnight
        }

        private static TimeBand GetTimeBand(DateTime timeOfToll)
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

        private static bool IsWeekDay(DateTime timeOfToll) =>
            timeOfToll.DayOfWeek switch
            {
                DayOfWeek.Saturday => false,
                DayOfWeek.Sunday => false,
                _ => true
            };

        #region Pattern_PeakTime
        public decimal PeakTimePremium(DateTime timeOfToll, bool inbound) =>
            (IsWeekDay(timeOfToll), GetTimeBand(timeOfToll), inbound) switch
            {
                (true, TimeBand.Overnight, _) => 0.75m,
                (true, TimeBand.Daytime, _) => 1.5m,
                (true, TimeBand.MorningRush, true) => 2.0m,
                (true, TimeBand.EveningRush, false) => 2.0m,
                (_, _, _) => 1.0m,
            };
        #endregion

    }

    class Patterns
    {
        internal int VehicleType()
        {
            #region Patterns_VehicleType
            var tollCalc = new TollCalculator_V1();

            var car = new Car();
            var taxi = new Taxi();
            var bus = new Bus();
            var truck = new DeliveryTruck();

            Console.WriteLine($"The toll for a car is {tollCalc.CalculateToll(car)}");
            Console.WriteLine($"The toll for a taxi is {tollCalc.CalculateToll(taxi)}");
            Console.WriteLine($"The toll for a bus is {tollCalc.CalculateToll(bus)}");
            Console.WriteLine($"The toll for a truck is {tollCalc.CalculateToll(truck)}");

            try
            {
                tollCalc.CalculateToll("this will fail");
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Caught an argument exception when using the wrong type");
            }
            try
            {
                tollCalc.CalculateToll(null);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("Caught an argument exception when using null");
            }
            #endregion
            return 0;
        }

        internal int TestOccupancy(string regionName)
        {

            ITollCalculator tollCalc = regionName switch
            {
                "Pattern_CarTaxiOccupancy"      => new TollCalculator_V2() as ITollCalculator,
                "Pattern_BusOccupancy"          => new TollCalculator_V3() as ITollCalculator,
                "Pattern_DeliveryTruckWeight"   => new TollCalculator_V4() as ITollCalculator,
                "Pattern_ChainedPatterns"       => new TollCalculator_V5() as ITollCalculator,
                _                               => new TollCalculator_V1() as ITollCalculator,
            };

            var soloDriver = new Car();
            var twoRideShare = new Car { Passengers = 1 };
            var threeRideShare = new Car { Passengers = 2 };
            var fullVan = new Car { Passengers = 5 };
            var emptyTaxi = new Taxi();
            var singleFare = new Taxi { Fares = 1 };
            var doubleFare = new Taxi { Fares = 2 };
            var fullVanPool = new Taxi { Fares = 5 };
            var lowOccupantBus = new Bus { Capacity = 90, Riders = 15 };
            var normalBus = new Bus { Capacity = 90, Riders = 75 };
            var fullBus = new Bus { Capacity = 90, Riders = 85 };

            var heavyTruck = new DeliveryTruck { GrossWeightClass = 7500 };
            var truck = new DeliveryTruck { GrossWeightClass = 4000 };
            var lightTruck = new DeliveryTruck { GrossWeightClass = 2500 };

            Console.WriteLine($"The toll for a solo driver is {tollCalc.CalculateToll(soloDriver)}");
            Console.WriteLine($"The toll for a two ride share is {tollCalc.CalculateToll(twoRideShare)}");
            Console.WriteLine($"The toll for a three ride share is {tollCalc.CalculateToll(threeRideShare)}");
            Console.WriteLine($"The toll for a fullVan is {tollCalc.CalculateToll(fullVan)}");

            Console.WriteLine($"The toll for an empty taxi is {tollCalc.CalculateToll(emptyTaxi)}");
            Console.WriteLine($"The toll for a single fare taxi is {tollCalc.CalculateToll(singleFare)}");
            Console.WriteLine($"The toll for a double fare taxi is {tollCalc.CalculateToll(doubleFare)}");
            Console.WriteLine($"The toll for a full van taxi is {tollCalc.CalculateToll(fullVanPool)}");

            Console.WriteLine($"The toll for a low-occupant bus is {tollCalc.CalculateToll(lowOccupantBus)}");
            Console.WriteLine($"The toll for a regular bus is {tollCalc.CalculateToll(normalBus)}");
            Console.WriteLine($"The toll for a bus is {tollCalc.CalculateToll(fullBus)}");

            Console.WriteLine($"The toll for a truck is {tollCalc.CalculateToll(heavyTruck)}");
            Console.WriteLine($"The toll for a truck is {tollCalc.CalculateToll(truck)}");
            Console.WriteLine($"The toll for a truck is {tollCalc.CalculateToll(lightTruck)}");

            try
            {
                tollCalc.CalculateToll("this will fail");
            }
            catch (ArgumentException e)
            {
                Console.WriteLine("Caught an argument exception when using the wrong type");
            }
            try
            {
                tollCalc.CalculateToll(null!);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("Caught an argument exception when using null");
            }
            return 1;
        }

        internal int PeakPricing()
        {
            var tollCalc = new TollCalculator_V5();

            var testTimes = new DateTime[]
            {
                new DateTime(2019, 3, 4, 8, 0, 0), // morning rush
                new DateTime(2019, 3, 6, 11, 30, 0), // daytime
                new DateTime(2019, 3, 7, 17, 15, 0), // evening rush
                new DateTime(2019, 3, 14, 03, 30, 0), // overnight

                new DateTime(2019, 3, 16, 8, 30, 0), // weekend morning rush
                new DateTime(2019, 3, 17, 14, 30, 0), // weekend daytime
                new DateTime(2019, 3, 17, 18, 05, 0), // weekend evening rush
                new DateTime(2019, 3, 16, 01, 30, 0), // weekend overnight
            };

            foreach (var time in testTimes)
            {
                Console.WriteLine($"Inbound premium at {time} is {tollCalc.PeakTimePremium(time, true)}");
                Console.WriteLine($"Outbound premium at {time} is {tollCalc.PeakTimePremium(time, false)}");
            }
            return 0;
        }

    }
}
