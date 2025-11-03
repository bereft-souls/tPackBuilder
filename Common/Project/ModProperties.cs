using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.Project;

public readonly struct ModProperties(BuildManifest manifest)
{
    /// <inheritdoc cref="BuildManifest.AssemblyReferences" />
    public IEnumerable<string> AssemblyReferences => manifest.AssemblyReferences;

    /// <inheritdoc cref="BuildManifest.StrongModReferences" />
    public IEnumerable<ModReference> StrongModReferences => manifest.StrongModReferences;

    /// <inheritdoc cref="BuildManifest.WeakModReferences" />
    public IEnumerable<ModReference> WeakModReferences => manifest.WeakModReferences;

    /// <inheritdoc cref="BuildManifest.ModsToSortAfter" />
    public IEnumerable<string> ModsToSortAfter => manifest.ModsToSortAfter;

    /// <inheritdoc cref="BuildManifest.ModsToSortBefore" />
    public IEnumerable<string> ModsToSortBefore => manifest.ModsToSortBefore;

    /// <inheritdoc cref="BuildManifest.Author" />
    public string Author => manifest.Author;

    /// <inheritdoc cref="BuildManifest.Version" />
    public Version Version => manifest.Version;

    /// <inheritdoc cref="BuildManifest.DisplayName" />
    public string DisplayName => manifest.DisplayName;

    /// <inheritdoc cref="BuildManifest.HomepageUrl" />
    public string HomepageUrl => manifest.HomepageUrl;

    /// <inheritdoc cref="BuildManifest.Side" />
    public ModSide Side => manifest.Side;
}
