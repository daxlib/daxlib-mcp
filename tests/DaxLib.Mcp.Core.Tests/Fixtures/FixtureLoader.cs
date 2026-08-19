using System.IO;
using System.Reflection;

namespace DaxLib.Mcp.Core.Tests.Fixtures;

/// <summary>
/// Loads the embedded TMDL fixtures: real <c>functions.tmdl</c> documents as published.
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
}
