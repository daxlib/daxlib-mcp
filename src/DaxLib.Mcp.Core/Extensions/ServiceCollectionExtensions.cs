using System;
using DaxLib.Client;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.SdkModeling;
using DaxLib.Mcp.Core.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace DaxLib.Mcp.Core.Extensions;

internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the DAX Lib MCP server and its tools to the service collection.
    /// </summary>
    public static IMcpServerBuilder AddDaxLibMcpServer(this IServiceCollection services)
    {
        services.AddDaxLibGallery();
        services.AddAnalysisServicesDiscovery();
        services.AddSingleton<PackageResolver>();
        services.AddSingleton<IPackageInstaller, PackageInstaller>();
        services.AddSingleton<ToolCallInvocation>();

        return services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new Implementation
                {
                    Name = McpServerInfo.Name,
                    Title = McpServerInfo.Title,
                    Version = McpServerInfo.Version,
                    Description = McpServerInfo.Description,
                    WebsiteUrl = McpServerInfo.WebsiteUrl,
                };
                options.ServerInstructions = McpServerInfo.Instructions;
            })
            .WithTools<SearchPackagesTool>()
            .WithTools<GetPackageTool>()
            .WithTools<GetPackageReadmeTool>()
            .WithTools<ListLocalInstancesTool>()
            .WithTools<InstallPackageTool>();
    }

    private static IServiceCollection AddDaxLibGallery(this IServiceCollection services)
    {
        // One long-lived client: DaxLibClient owns an HttpClient and is not built on
        // IHttpClientFactory, so creating one per request would exhaust sockets.
        services.AddSingleton(_ =>
        {
            var productInfo = new ProductInformation(McpServerInfo.Name, McpServerInfo.Version);
            return new DaxLibClient(productInfo);
        });

        services.AddSingleton<IGalleryClient, GalleryClient>();
        services.AddSingleton<IPackageProvider, PackageProvider>();

        return services;
    }

    private static IServiceCollection AddAnalysisServicesDiscovery(this IServiceCollection services)
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
            services.AddSingleton<IAnalysisServicesInstanceLocator, AnalysisServicesInstanceLocator>();
        else
            services.AddSingleton<IAnalysisServicesInstanceLocator, NullAnalysisServicesInstanceLocator>();

        return services;
    }
}
