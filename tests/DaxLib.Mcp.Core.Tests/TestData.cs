using System;
using System.Collections.Generic;
using System.Linq;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Packaging;
using Xunit;

namespace DaxLib.Mcp.Core.Tests;

/// <summary>
/// Shared factories for the domain values the tests exercise. Only the fields a test varies are
/// parameters. The others are fixed, so a package reads the same wherever it appears.
/// </summary>
internal static class TestData
{
    public static PackageId Id(string value) => PackageId.Parse(value);

    public static PackageVersion Version(string value)
    {
        Assert.True(PackageVersion.TryParse(value, out var version));

        return version;
    }

    public static GalleryPackage Package(string id = "DaxLib.SVG", string version = "2.0.0") => new()
    {
        Id = Id(id),
        Version = Version(version),
        Description = "A DAX UDF library for SVG visuals.",
        Authors = ["SQLBI"],
        Owners = ["daxlib"],
        Tags = ["DAX", "UDF", "SVG"],
        ReleaseNotes = string.Empty,
        Published = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
        Downloads = 11_623,
        HasReadme = true,
    };

    public static IReadOnlyList<PackageVersion> Versions(params string[] values) =>
        [.. values.Select(Version)];
}
