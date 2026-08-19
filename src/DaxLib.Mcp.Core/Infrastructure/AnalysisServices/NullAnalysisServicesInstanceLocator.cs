using System.Collections.Generic;
using System.Threading;

namespace DaxLib.Mcp.Core.Infrastructure.AnalysisServices;

/// <summary>
/// An implementation of <see cref="IAnalysisServicesInstanceLocator"/> that never returns an instance.
/// </summary>
internal sealed class NullAnalysisServicesInstanceLocator : IAnalysisServicesInstanceLocator
{
    public IReadOnlyList<AnalysisServicesInstance> FindInstances(CancellationToken cancellationToken) => [];

    public AnalysisServicesInstance? FindInstance(string dataSource, CancellationToken cancellationToken) => null;
}
