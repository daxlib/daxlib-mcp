using System.IO;
using System.Reflection;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.Tests.Fixtures;

/// <summary>
/// Loads the embedded fixtures: real <c>functions.tmdl</c> documents and <c>.daxpkg</c> packages as
/// published, and the expected results of the investigation (<c>Expected/</c>).
/// </summary>
internal static class FixtureLoader
{
    public static Stream Open(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"{typeof(FixtureLoader).Namespace}.{fileName}";

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded fixture '{resourceName}' was not found.");
    }

    public static string ReadText(string fileName)
    {
        using var stream = Open(fileName);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }

    /// <summary>
    /// Opens a published package from <c>Fixtures/Daxpkg</c>, e.g. <c>OpenPackage("DaxLib.Sample", "0.1.8")</c>.
    /// </summary>
    public static Package OpenPackage(string packageId, string version)
    {
        using var stream = Open($"Daxpkg.{packageId}.{version}.daxpkg");

        // Package.Open requires a seekable stream that it owns.
        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        buffer.Position = 0;

        return Package.Open(buffer);
    }
}
