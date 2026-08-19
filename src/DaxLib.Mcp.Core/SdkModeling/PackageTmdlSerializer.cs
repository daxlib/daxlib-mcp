using System.Collections.Generic;
using DaxLib.Packaging;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.AnalysisServices.Tabular.Serialization;

namespace DaxLib.Mcp.Core.SdkModeling;

/// <summary>
/// Deserializes the TMDL a DaxLib package publishes into a detached in-memory TOM model.
/// </summary>
internal static class PackageTmdlSerializer
{
    private static readonly MetadataDeserializationOptions s_deserializationOptions =
        new MetadataDeserializationOptionsBuilder(MetadataSerializationStyle.Tmdl)
            .WithCompatibilityOptions(new MetadataCompatibilityOptionsBuilder()
                .WithoutTargetCompatibilityMode()
                .WithoutTargetCompatibilityLevel()
                .GetOptions())
            .WithErrorOnUnresolvedLinks()
            .GetOptions();

    public static Model DeserializeModel(Package package)
        => DeserializeModel(package.GetDocuments());

    internal static Model DeserializeModel(IReadOnlyList<LibraryDocument> documents)
    {
        var context = MetadataSerializationContext.Create(MetadataSerializationStyle.Tmdl);

        foreach (var document in documents)
        {
            using var content = document.GetStream();

            context.ReadFromDocument(document.Path, content);
        }

        return context.ToModel(s_deserializationOptions);
    }
}
