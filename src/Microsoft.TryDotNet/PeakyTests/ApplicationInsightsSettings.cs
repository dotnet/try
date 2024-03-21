// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Microsoft.TryDotNet.PeakyTests;

[ShortName("Insights")]
internal class ApplicationInsightsSettings : IEquatable<ApplicationInsightsSettings>
{
    public string WriteKey { get; set; }
    public string ReadKey { get; set; }
    public string AppId { get; set; }
    public bool DeveloperMode { get; set; }

    public override bool Equals(object obj) => Equals(obj as ApplicationInsightsSettings);
    public override int GetHashCode() => throw new NotSupportedException();

    public static ApplicationInsightsSettings ForLocal => new() { AppId = "local", ReadKey = "ReadKey", WriteKey = "WriteKey" };

    public bool Equals(ApplicationInsightsSettings other) =>
        other != null &&
        WriteKey.Equals(other.WriteKey) &&
        ReadKey.Equals(other.ReadKey) &&
        AppId.Equals(other.AppId) &&
        DeveloperMode == other.DeveloperMode;

    public override string ToString() => $"Key: {WriteKey}, ApiKey: secret, DeveloperMode: {DeveloperMode}";
}