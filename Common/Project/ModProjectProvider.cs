using System;
using System.IO;
using System.Linq;
using PackBuilder.Common.Project.IO;
using PackBuilder.Common.Project.ManifestFormats;
using Terraria.ModLoader.Core;

namespace PackBuilder.Common.Project;

/// <summary>
///     Provides access to <see cref="ModProject"/> objects.
/// </summary>
public static class ModProjectProvider
{
    public static bool TryGetFromModSources(string modName, out ModProject project)
    {
        var modSources = ModCompile.FindModSources();
        var matchedSource = modSources.FirstOrDefault(x => string.Equals(Path.GetFileName(x), modName, StringComparison.InvariantCultureIgnoreCase));
        if (matchedSource is null)
        {
            project = default(ModProject);
            return false;
        }

        return TryGetFromDirectory(matchedSource, out project);
    }

    public static bool TryGetFromDirectory(string directory, out ModProject project)
    {
        if (!Directory.Exists(directory))
        {
            project = default(ModProject);
            return false;
        }

        // TODO: Un-hardcode manifest reading!!!
        var modSource = new DirectoryModSource(directory);
        var manifest = WellKnownBuildManifestFormats.BuildTxt.Deserialize(modSource);
        if (manifest is null)
        {
            project = default(ModProject);
            return false;
        }

        project = new ModProject(modSource, manifest);
        return true;
    }
}
