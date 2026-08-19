using DaxLib.Packaging;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

internal sealed partial class GalleryClient
{
    [LoggerMessage(
        EventId = 2000,
        EventName = "GallerySearchStarted",
        Level = LogLevel.Debug,
        Message = "Gallery search starting. Query: '{Query}', skip: {Skip}, take: {Take}, prerelease: {IncludePrerelease}.")]
    private partial void LogSearchStarted(string query, int skip, int take, bool includePrerelease);

    // The query text is logged as feedback on search quality. It is not personal data.
    [LoggerMessage(
        EventId = 2001,
        EventName = "GallerySearchCompleted",
        Level = LogLevel.Information,
        Message = "Gallery search completed. Query: '{Query}', results: {ResultCount}.")]
    private partial void LogSearchCompleted(string query, int resultCount);

    [LoggerMessage(
        EventId = 2002,
        EventName = "GalleryPackageDownloadStarted",
        Level = LogLevel.Information,
        Message = "Downloading package {PackageId} {PackageVersion} from the gallery.")]
    private partial void LogPackageDownloadStarted(PackageId packageId, PackageVersion packageVersion);

    [LoggerMessage(
        EventId = 2003,
        EventName = "GalleryPackageVersionsCompleted",
        Level = LogLevel.Information,
        Message = "Gallery package-version lookup completed for {PackageId}. Versions: {VersionCount}.")]
    private partial void LogPackageVersionsCompleted(PackageId packageId, int versionCount);

    [LoggerMessage(
        EventId = 2004,
        EventName = "GalleryPackageMetadataCompleted",
        Level = LogLevel.Information,
        Message = "Gallery package metadata lookup completed for {PackageId} {PackageVersion}. Found: {Found}.")]
    private partial void LogPackageMetadataCompleted(
        PackageId packageId,
        PackageVersion packageVersion,
        bool found);

    [LoggerMessage(
        EventId = 2005,
        EventName = "GalleryPackageReadmeCompleted",
        Level = LogLevel.Information,
        Message = "Gallery package README lookup completed for {PackageId} {PackageVersion}. Found: {Found}; characters: {CharacterCount}.")]
    private partial void LogPackageReadmeCompleted(
        PackageId packageId,
        PackageVersion packageVersion,
        bool found,
        int characterCount);
}
