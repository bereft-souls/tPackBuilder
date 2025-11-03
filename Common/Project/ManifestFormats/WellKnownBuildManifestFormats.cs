namespace PackBuilder.Common.Project.ManifestFormats;

/// <summary>
///     Provides well-known build manifest format presets.
/// </summary>
public static class WellKnownBuildManifestFormats
{
    public static IBuildManifestFormat BuildTxt { get; } = new BuildTxtManifestFormat();
}
