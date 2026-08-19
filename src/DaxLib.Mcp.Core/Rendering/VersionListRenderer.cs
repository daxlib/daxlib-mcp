using System.Collections.Generic;
using System.Linq;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.Rendering;

internal static class VersionListRenderer
{
    public static IReadOnlyList<string> Render(IReadOnlyList<PackageVersion> versions)
    {
        return
        [
            .. versions.Select((version, index) =>
            {
                var text = version.ToNormalizedString();

                if (index == 0)
                    return $"{text} (latest)";

                return version.IsPrerelease ? $"{text} (prerelease)" : text;
            }),
        ];
    }
}
