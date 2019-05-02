using System;
using System.Threading.Tasks;

namespace ExploreCsharpEight
{
    // These are my sample creation notes, with the goal
    // of making a set of recommendations for upcoming tutorials (online or local)

    // Rule 1: Make a new class for each section.
    // Rule 2: Each section should have two parts:
        // a. Basic syntax for the new feature.
        // b. A scenario for using the feature.

    // Questions:
    // What is "package" used for?
    // How to include non-editable blocks
    class Program
    {
#nullable enable
        static async Task<int> Main(string? region = null,
            string? session = null,
            string? package = null,
            string? project = null,
            string[]? args = null)
        {
            return region switch
            {
                "Pattern_CalculateToll"             => new Patterns().VehicleType(),
                "Pattern_CarTaxiOccupancy"          => new Patterns().TestOccupancy("Pattern_CarTaxiOccupancy"),
                "Pattern_BusOccupancy"              => new Patterns().TestOccupancy("Pattern_BusOccupancy"),
                "Pattern_DeliveryTruckWeight"       => new Patterns().TestOccupancy("Pattern_DeliveryTruckWeight"),
                "Pattern_ChainedPatterns"           => new Patterns().TestOccupancy("Pattern_ChainedPatterns"),
                "Pattern_PeakTime"                  => new Patterns().PeakPricing(),

                "LocalFunction_Counting"            => new StaticLocalFunctions().LocalFunctionWithCapture(),

                "Using_Block"                       => new UsingDeclarationsRefStruct().OldStyle(),
                "Using_Declaration"                 => new UsingDeclarationsRefStruct().NewStyle(),

                "Nullable_PersonDefinition"         => new NullableReferences().NullableTestBed(),
                "Nullable_GetLengthMethod"          => new NullableReferences().NullableTestBed(),
                "Nullable_Usage"                    => new NullableReferences().NullableTestBed(),

                "AsyncStreams_Declare"              => await new AsyncStreams().ConsumeStream(),
                "AsyncStreams_Consume"              => await new AsyncStreams().ConsumeStream(),

                "IndicesAndRanges_LastIndex"        => new IndicesAndRanges().Syntax_LastIndex(),
                "IndicesAndRanges_Range"            => new IndicesAndRanges().Syntax_Range(),
                "IndicesAndRanges_LastRange"        => new IndicesAndRanges().Syntax_LastRange(),
                "IndicesAndRanges_PartialRanges"    => new IndicesAndRanges().Syntax_PartialRange(),
                "IndicesAndRanges_RangeIndexTypes"  => new IndicesAndRanges().Syntax_IndexRangeType(),
                "IndicesAndRanges_CreateRange"      => new IndicesAndRanges().Syntax_WhyChosenSemantics(),
                "IndicesAndRanges_MathWithLength"   => new IndicesAndRanges().Syntax_WhyChosenSemantics(),
                "IndicesAndRanges_Disjoint"         => new IndicesAndRanges().Syntax_WhyChosenSemantics(),
                "IndicesAndRanges_RemoveFromEnds"   => new IndicesAndRanges().Syntax_WhyChosenSemantics(),
                "IndicesAndRanges_IncompleteRanges" => new IndicesAndRanges().ComputeMovingAverages(),
                "IndicesAndRanges_MovingAverage"    => new IndicesAndRanges().ComputeMovingAverages(),
                null => await RunAll(),
                _ => await RunAll()
            };
        }
#nullable restore

        private static async Task<int> RunAll()
        {
            Console.WriteLine("==========          Starting pattern matching samples.         ==========");
            var patterns = new Patterns();
            Console.WriteLine("          ==========          Run vehicle type.       ==========");
            patterns.VehicleType();
            Console.WriteLine("          ==========          Run car taxi occupancy. ==========");
            patterns.TestOccupancy("Pattern_CarTaxiOccupancy");
            Console.WriteLine("          ==========          Run bus occupancy.      ==========");
            patterns.TestOccupancy("Pattern_BusOccupancy");
            Console.WriteLine("          ==========          Run delivery truck.     ==========");
            patterns.TestOccupancy("Pattern_DeliveryTruckWeight");
            Console.WriteLine("          ==========          Run chained patterns.   ==========");
            patterns.TestOccupancy("Pattern_ChainedPatterns");
            Console.WriteLine("          ==========          Run peak pricing.       ==========");
            patterns.PeakPricing();

            Console.WriteLine("==========          Starting static local functions.           ==========");
            var localFuncs = new StaticLocalFunctions();
            Console.WriteLine("          ==========          Run local func capture. ==========");
            localFuncs.LocalFunctionWithCapture();
            Console.WriteLine("          ==========          Run local static func.  ==========");
            localFuncs.LocalFunctionWithNoCapture();

            Console.WriteLine("==========          Starting using declaration / ref struct.   ==========");
            var usings = new UsingDeclarationsRefStruct();
            Console.WriteLine("          ==========          Run using block style.  ==========");
            usings.OldStyle();
            Console.WriteLine("          ==========          Run declaration style.  ==========");
            usings.NewStyle();

            Console.WriteLine("==========          Starting nullable reference samples.       ==========");
            var nullableSamples = new NullableReferences();
            Console.WriteLine("          ==========          Run nullable.          ==========");
            try
            {
                nullableSamples.NullableTestBed();
            }catch (NullReferenceException)
            {
                Console.WriteLine("Initial nullable sample threw the expected NullReferenceException");
            }
            Console.WriteLine("==========          Starting Async Stream Samples.             ==========");
            var asyncSamples = new AsyncStreams();
            Console.WriteLine("          ==========          Generate sequence.     ==========");
            await asyncSamples.ConsumeStream();

            Console.WriteLine("==========          Starting Index and Range Samples.          ==========");
            var indexSamples = new IndicesAndRanges();
            Console.WriteLine("          ==========          Last Index.            ==========");
            indexSamples.Syntax_LastIndex();
            Console.WriteLine("          ==========          Range.                 ==========");
            indexSamples.Syntax_Range();
            Console.WriteLine("          ==========          Last Range.            ==========");
            indexSamples.Syntax_LastRange();
            Console.WriteLine("          ==========          Partial Range.         ==========");
            indexSamples.Syntax_PartialRange();
            Console.WriteLine("          ==========          Index and Range types. ==========");
            indexSamples.Syntax_IndexRangeType();
            Console.WriteLine("          ==========          Why this syntax.       ==========");
            indexSamples.Syntax_WhyChosenSemantics();
            Console.WriteLine("          ==========          Scenario.              ==========");
            indexSamples.ComputeMovingAverages();
            return 0;
        }
    }
}
