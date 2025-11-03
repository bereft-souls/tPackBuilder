using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    public static IEnumerable<ModProjectView> ModSourcesViews
    {
        get
        {
            var modSources = ModCompile.FindModSources();
            foreach (var modSource in modSources)
            {
                if (TryGetFromDirectory(modSource, out var project))
                    yield return new ModProjectView(project);
            }
        }
    }

    public static bool TryGetFromModSources(
        string modName,
        [NotNullWhen(returnValue: true)] out ModProject? project
    )
    {
        var modSources = ModCompile.FindModSources();
        var matchedSource = modSources.FirstOrDefault(x => string.Equals(Path.GetFileName(x), modName, StringComparison.InvariantCultureIgnoreCase));
        if (matchedSource is null)
        {
            project = null;
            return false;
        }

        return TryGetFromDirectory(matchedSource, out project);
    }

    public static bool TryCreateInModSources(
        string modName,
        [NotNullWhen(returnValue: true)] out ModProject? project
    )
    {
        var path = Path.Combine(ModCompile.ModSourcePath, modName);
        return TryCreateDirectory(path, out project);
    }

    public static bool TryGetFromDirectory(
        string directory,
        [NotNullWhen(returnValue: true)] out ModProject? project
    )
    {
        if (!Directory.Exists(directory))
        {
            project = null;
            return false;
        }

        // TODO: Un-hardcode manifest reading!!!
        var modSource = new DirectoryModSource(directory);
        var manifest = WellKnownBuildManifestFormats.BuildTxt.Deserialize(modSource);
        if (manifest is null)
        {
            project = null;
            return false;
        }

        project = new ModProject(modSource, manifest);
        return true;
    }

    public static bool TryCreateDirectory(
        string directory,
        [NotNullWhen(returnValue: true)] out ModProject? project
    )
    {
        if (Directory.Exists(directory))
        {
            project = null;
            return false;
        }

        if (File.Exists(directory))
        {
            project = null;
            return false;
        }

        Directory.CreateDirectory(directory);

        var modSource = new DirectoryModSource(directory);
        var manifest = new BuildManifest();
        project = new ModProject(modSource, manifest);
        return true;
    }
}
