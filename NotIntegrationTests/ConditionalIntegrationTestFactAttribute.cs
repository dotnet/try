using System;
using Xunit;

namespace NotIntegrationTests
{
    public class ConditionalIntegrationTestFactAttribute : FactAttribute
    {
        const string RUN_DOTNET_TRY_INTEGRATION_TESTS = nameof(RUN_DOTNET_TRY_INTEGRATION_TESTS);

        public ConditionalIntegrationTestFactAttribute()
        {
            var variable = Environment.GetEnvironmentVariable(RUN_DOTNET_TRY_INTEGRATION_TESTS);
            if (!bool.TryParse(variable, out var value) || !value)
            {
                base.Skip = base.Skip ?? $"Integration tests disabled. Set {RUN_DOTNET_TRY_INTEGRATION_TESTS}=true to run them";
            }
        }
    }
}
