using System.Collections.Generic;
using System.Threading;

namespace DaxLib.Mcp.Core.Infrastructure.AnalysisServices;

/// <summary>
/// Locates running Analysis Services instances on the local machine.
/// </summary>
internal interface IAnalysisServicesInstanceLocator
{
    IReadOnlyList<AnalysisServicesInstance> FindInstances(CancellationToken cancellationToken);

    /// <summary>
    /// Finds a running Analysis Services instance by its data source name.
    /// </summary>
    AnalysisServicesInstance? FindInstance(string dataSource, CancellationToken cancellationToken);
}
