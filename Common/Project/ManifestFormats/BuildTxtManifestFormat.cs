using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PackBuilder.Common.Project.IO;
using Terraria.ModLoader;

namespace PackBuilder.Common.Project.ManifestFormats;

/// <summary>
///     Implements the <c>build.txt</c> format.
/// </summary>
internal sealed class BuildTxtManifestFormat : IBuildManifestFormat
{
    void IBuildManifestFormat.Serialize(BuildManifest manifest, IModSource source)
    {
        var sb = new StringBuilder();

        if (manifest.AssemblyReferences.Count > 0)
        {
            WriteList(sb, "dllReferences", manifest.AssemblyReferences);
        }

        if (manifest.StrongModReferences.Count > 0)
        {
            WriteList(sb, "modReferences", manifest.StrongModReferences);
        }

        if (manifest.WeakModReferences.Count > 0)
        {
            WriteList(sb, "weakReferences", manifest.WeakModReferences);
        }

        if (manifest.ModsToSortBefore.Count > 0)
        {
            WriteList(sb, "sortBefore", manifest.ModsToSortBefore);
        }

        if (manifest.ModsToSortAfter.Count > 0)
        {
            WriteList(sb, "sortAfter", manifest.ModsToSortAfter);
        }

        sb.AppendLine($"version = {manifest.Version}");

        if (!string.IsNullOrWhiteSpace(manifest.Author))
        {
            sb.AppendLine($"author = {manifest.Author}");
        }

        if (!string.IsNullOrWhiteSpace(manifest.DisplayName))
        {
            sb.AppendLine($"displayName = {manifest.DisplayName}");
        }

        if (!string.IsNullOrWhiteSpace(manifest.HomepageUrl))
        {
            sb.AppendLine($"homepage = {manifest.HomepageUrl}");
        }

        if (manifest.IgnoredBuildPaths.Count > 0)
        {
            WriteList(sb, "buildIgnore", manifest.IgnoredBuildPaths);
        }

        sb.AppendLine($"side = {manifest.Side}");

        var dir = source.GetDirectory().FullName;
        File.WriteAllText(Path.Combine(dir, "build.txt"), sb.ToString());

        if (!string.IsNullOrWhiteSpace(manifest.Description))
        {
            File.WriteAllText(Path.Combine(dir, "description.txt"), manifest.Description);
        }

        return;

        static void WriteList<T>(StringBuilder sb, string property, IEnumerable<T> values)
        {
            sb.AppendLine($"{property} = {string.Join(',', values)}");
        }
    }

    BuildManifest? IBuildManifestFormat.Deserialize(IModSource source)
    {
        var dir = source.GetDirectory();
        var buildTxtPath = Path.Combine(dir.FullName, "build.txt");
        var descriptionPath = Path.Combine(dir.FullName, "description.txt");

        if (!File.Exists(buildTxtPath))
        {
            return null;
        }

        var manifest = new BuildManifest();
        if (File.Exists(descriptionPath))
        {
            manifest.Description = File.ReadAllText(descriptionPath);
        }

        foreach (var line in File.ReadAllLines(buildTxtPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var split = line.Split('=', 2);
            if (split.Length != 2)
                continue;

            var property = split[0].Trim();
            var value = split[1].Trim();
            if (value.Length == 0)
                continue;

            switch (property)
            {
                case "dllReferences":
                    manifest.AssemblyReferences.AddRange(ReadList(value));
                    break;

                case "modReferences":
                    var strongRefs = new List<ModReference>();
                    foreach (var modRefVal in ReadList(value))
                    {
                        if (ModReference.TryParse(modRefVal, out var modRef))
                            strongRefs.Add(modRef);
                    }

                    manifest.StrongModReferences.AddRange(strongRefs);
                    break;

                case "weakReferences":
                    var weakRefs = new List<ModReference>();
                    foreach (var modRefVal in ReadList(value))
                    {
                        if (ModReference.TryParse(modRefVal, out var modRef))
                            weakRefs.Add(modRef);
                    }

                    manifest.WeakModReferences.AddRange(weakRefs);
                    break;

                case "sortBefore":
                    manifest.ModsToSortBefore.AddRange(ReadList(value));
                    break;

                case "sortAfter":
                    manifest.ModsToSortAfter.AddRange(ReadList(value));
                    break;

                case "author":
                    manifest.Author = value;
                    break;

                case "version":
                    if (Version.TryParse(value, out var modVersion))
                    {
                        manifest.Version = modVersion;
                    }

                    break;

                case "displayName":
                    manifest.DisplayName = value;
                    break;

                case "homepage":
                    manifest.HomepageUrl = value;
                    break;

                case "noCompile":
                    manifest.NoCompile = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "playableOnPreview":
                    manifest.PlayableOnPreview = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "translationMod":
                    manifest.TranslationMod = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "hideCode":
                    manifest.HideCode = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "hideResources":
                    manifest.HideResources = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "includeSource":
                    manifest.IncludeSource = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                    break;

                case "buildIgnore":
                    manifest.IgnoredBuildPaths.AddRange(
                        value.Split(',')
                             .Select(
                                  x => x.Trim()
                                        .Replace('\\', Path.DirectorySeparatorChar)
                                        .Replace('/', Path.DirectorySeparatorChar)
                              )
                             .Where(x => x.Length > 0)
                    );
                    break;

                case "side":
                    if (Enum.TryParse<ModSide>(value, out var modSide))
                    {
                        manifest.Side = modSide;
                    }

                    break;
            }
        }

        // All of these tasks are covered during actual building so are low
        // priority:
        // TODO: Check for duplicate mod/weak references.
        // TODO: Check for duplicate dllReferences/modReferences.
        // TODO: Add mod/weakReferences that are not sortBefore to sortAfter.

        // TODO: Should we format description values or leave it as raw text to
        //       potentially let the user edit?

        return manifest;

        static IEnumerable<string> ReadList(string value)
        {
            return value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}
