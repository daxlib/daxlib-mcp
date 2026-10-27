using System;
using System.Collections.Frozen;
using System.IO;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// The names of the DAX built-in functions, so that call detection can tell them from package
/// functions. The list includes the dotted names, such as <c>NORM.S.DIST</c>, that the lexer
/// reads as one identifier.
/// </summary>
/// <remarks>
/// <c>DaxBuiltInFunctions.txt</c> is the output of <c>INFO.FUNCTIONS</c>, one name per line. A
/// built-in function that is missing from the list is reported as an unresolved call, which never
/// marks a DEFINE block as incomplete.
/// </remarks>
internal static class DaxBuiltInFunctions
{
    private const string ResourceName = "DaxLib.Mcp.Core.QueryDefinitions.DaxBuiltInFunctions.txt";

    private static readonly FrozenSet<string> s_names = Load();

    public static bool Contains(string name) => s_names.Contains(name);

    private static FrozenSet<string> Load()
    {
        using var stream = typeof(DaxBuiltInFunctions).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);

        var names = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return names.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }
}
