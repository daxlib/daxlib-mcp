using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Tools;

internal sealed partial class GetFunctionDefinitionsTool
{
    [LoggerMessage(
        EventId = 8000,
        EventName = "FunctionDefinitionsCompleted",
        Level = LogLevel.Information,
        Message = "Function definitions built. Functions: {FunctionCount}; packages: {PackageCount}; warnings: {WarningCount}; complete: {Complete}; characters: {CharacterCount}.")]
    private partial void LogDefinitionsCompleted(
        int functionCount,
        int packageCount,
        int warningCount,
        bool complete,
        int characterCount);
}
