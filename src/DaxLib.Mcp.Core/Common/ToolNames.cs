namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// The MCP tool names exposed by this server. They are part of the published contract: clients
/// reference them in their configuration, so renaming a tool is a breaking change.
/// </summary>
internal static class ToolNames
{
    public const string SearchPackages = "search_packages";
    public const string GetPackage = "get_package";
    public const string GetPackageReadme = "get_package_readme";
    public const string ListLocalInstances = "list_local_instances";
    public const string InstallPackage = "install_package";
}
