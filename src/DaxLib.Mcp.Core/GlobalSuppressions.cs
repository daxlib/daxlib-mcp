// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Any TOM failure must become a PackageInstallOutcome, not an exception; the teardown catch must never throw.", Scope = "member", Target = "~M:DaxLib.Mcp.Core.SdkModeling.PackageInstaller.Install(DaxLib.Packaging.Package,DaxLib.Mcp.Core.SdkModeling.ConnectionInfo)~DaxLib.Mcp.Core.SdkModeling.PackageInstallOutcome")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The MCP boundary converts unexpected exceptions into tool results and logs their diagnostics.", Scope = "member", Target = "~M:DaxLib.Mcp.Core.Common.ToolCallInvocation.RunAsync(System.String,System.Func{System.Threading.Tasks.Task{DaxLib.Mcp.Core.Common.ToolOutcome}})~System.Threading.Tasks.Task{ModelContextProtocol.Protocol.CallToolResult}")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The Server is disposed in the finally with teardown failures deliberately tolerated: after SaveChanges has committed, a Dispose throw must not override the Installed outcome.", Scope = "member", Target = "~M:DaxLib.Mcp.Core.SdkModeling.PackageInstaller.Install(DaxLib.Packaging.Package,DaxLib.Mcp.Core.SdkModeling.ConnectionInfo)~DaxLib.Mcp.Core.SdkModeling.PackageInstallOutcome")]
