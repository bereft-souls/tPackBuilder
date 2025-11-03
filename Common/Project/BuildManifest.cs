using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.Project;

/// <summary>
///     A mutable structure encompassing data regarding mod project build
///     metadata.
/// </summary>
public sealed class BuildManifest
{
    /// <summary>
    ///     List of assembly references in <c>lib/</c>.
    ///     <br />
    ///     Corresponds to <c>dllReferences</c>.
    /// </summary>
    public List<string> AssemblyReferences { get; } = [];

    /// <summary>
    ///     List of strongly-referenced mod names.
    ///     <br />
    ///     Corresponds to <c>modReferences</c>.
    /// </summary>
    public List<ModReference> StrongModReferences { get; } = [];

    /// <summary>
    ///     List of weakly-referenced mod names.
    ///     <br />
    ///     Corresponds to <c>weakReferences</c>.
    /// </summary>
    public List<ModReference> WeakModReferences { get; } = [];

    /// <summary>
    ///     List of mod names that this mod should be sorted before.
    ///     <br />
    ///     Corresponds to <c>sortAfter</c>.
    /// </summary>
    public List<string> ModsToSortAfter { get; } = [];

    /// <summary>
    ///     List of mod names that this mod should be sorted after.
    ///     <br />
    ///     Corresponds to <c>sortBefore</c>.
    /// </summary>
    public List<string> ModsToSortBefore { get; } = [];

    /// <summary>
    ///     List of path globs to ignore, including explicitly-named files and
    ///     directories as well as <c>*</c> (asterisk) wildcards.
    ///     <br />
    ///     Corresponds to <c>buildIgnore</c>.
    /// </summary>
    public List<string> IgnoredBuildPaths { get; } = [];

    /// <summary>
    ///     The author of the mod.
    ///     <br />
    ///     Corresponds to <c>author</c>.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    ///     The version of the mod.
    ///     <br />
    ///     Corresponds to <c>version</c>.
    /// </summary>
    public Version Version { get; set; } = default_version;

    /// <summary>
    ///     The display (not internal!) name of the mod.
    ///     <br />
    ///     Corresponds to <c>displayName</c>.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    ///     The URL to the mod's homepage.
    ///     <br />
    ///     Corresponds to <c>homepage</c>.
    /// </summary>
    public string HomepageUrl { get; set; } = string.Empty;

    /// <summary>
    ///     Determines how a mod is synced across clients and the server,
    ///     notably impacting which instance needs the mod and whether clients
    ///     may have it enabled without the server.  See <see cref="ModSide"/>
    ///     for details.
    ///     <br />
    ///     Corresponds to <c>side</c>.
    /// </summary>
    public ModSide Side { get; set; }

    /// <summary>
    ///     Whether this mod is playable on preview versions of tModLoader.
    ///     <br />
    ///     Corresponds to <c>playableOnPreview</c>.
    /// </summary>
    public bool PlayableOnPreview { get; set; } = true;

    /// <summary>
    ///     Whether this mod is specifically designed to provide translations
    ///     for other mods.
    ///     <br />
    ///     Corresponds to <c>translationMod</c>.
    /// </summary>
    public bool TranslationMod { get; set; } = false;

    // Currently unsupported.
    internal bool NoCompile { get; set; } = false;

    // Currently unsupported.
    internal bool HideCode { get; set; } = false;

    // Currently unsupported.
    internal bool HideResources { get; set; } = false;

    // Currently unsupported.
    internal bool IncludeSource { get; set; } = false;

    // Intentionally internal.
    internal string EacPath { get; set; } = string.Empty;

    // Intentionally internal.
    internal Version ModLoaderVersion { get; set; } = BuildInfo.tMLVersion;
    
    // Intentionally internal.
    internal string Description { get; set; } = string.Empty;
    
    // Intentionally internal.
    internal string ModSource { get; set; } = string.Empty;

    private static readonly Version default_version = new(1, 0, 0, 0);
}
