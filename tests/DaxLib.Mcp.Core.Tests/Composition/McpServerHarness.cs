using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Extensions;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.SdkModeling;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NSubstitute;

namespace DaxLib.Mcp.Core.Tests.Composition;

/// <summary>
/// Runs the real DI composition (<c>AddDaxLibMcpServer</c>, tools, filters) against an in-memory
/// client over a pair of pipes, with every external dependency substituted, so that no test
/// touches the network or a live Analysis Services instance.
/// </summary>
internal sealed class McpServerHarness : IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly StreamServerTransport _serverTransport;
    private readonly McpServer _server;
    private readonly Task _serverRun;

    public required IGalleryClient GalleryClient { get; init; }

    public required IPackageProvider PackageProvider { get; init; }

    public required IAnalysisServicesInstanceLocator InstanceLocator { get; init; }

    public required IPackageInstaller Installer { get; init; }

    public required McpClient Client { get; init; }

    private McpServerHarness(
        ServiceProvider serviceProvider,
        StreamServerTransport serverTransport,
        McpServer server,
        Task serverRun)
    {
        _serviceProvider = serviceProvider;
        _serverTransport = serverTransport;
        _server = server;
        _serverRun = serverRun;
    }

    public static async Task<McpServerHarness> StartAsync(
        CancellationToken cancellationToken,
        Action<IServiceCollection>? configureServices = null)
    {
        var galleryClient = Substitute.For<IGalleryClient>();
        var packageProvider = Substitute.For<IPackageProvider>();
        var instanceLocator = Substitute.For<IAnalysisServicesInstanceLocator>();
        var installer = Substitute.For<IPackageInstaller>();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDaxLibMcpServer();
        services.RemoveAll<IGalleryClient>();
        services.RemoveAll<IPackageProvider>();
        services.RemoveAll<IAnalysisServicesInstanceLocator>();
        services.RemoveAll<IPackageInstaller>();
        services.AddSingleton(galleryClient);
        services.AddSingleton(packageProvider);
        services.AddSingleton(instanceLocator);
        services.AddSingleton(installer);
        configureServices?.Invoke(services);

        var serviceProvider = services.BuildServiceProvider(validateScopes: true);

        var clientToServer = new Pipe();
        var serverToClient = new Pipe();

        var serverTransport = new StreamServerTransport(
            clientToServer.Reader.AsStream(),
            serverToClient.Writer.AsStream(),
            "daxlib-mcp-tests",
            loggerFactory: null);

        var options = serviceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        var server = McpServer.Create(serverTransport, options, serviceProvider.GetService<ILoggerFactory>(), serviceProvider);
        var serverRun = server.RunAsync(cancellationToken);

        var client = await McpClient.CreateAsync(
            new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream(), loggerFactory: null),
            cancellationToken: cancellationToken);

        return new McpServerHarness(serviceProvider, serverTransport, server, serverRun)
        {
            GalleryClient = galleryClient,
            PackageProvider = packageProvider,
            InstanceLocator = instanceLocator,
            Installer = installer,
            Client = client,
        };
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await _server.DisposeAsync();
        await _serverTransport.DisposeAsync();

        try
        {
            await _serverRun;
        }
        catch (OperationCanceledException)
        {
        }

        await _serviceProvider.DisposeAsync();
    }
}
