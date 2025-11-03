using PackBuilder.Common.Project.IO;

namespace PackBuilder.Common.Project.ManifestFormats;

/// <summary>
///     Implements the <c>build.txt</c> format.
/// </summary>
internal sealed class BuildTxtManifestFormat : IBuildManifestFormat
{
    void IBuildManifestFormat.Serialize(BuildManifest manifest, IModSource source)
    {
    }

    BuildManifest? IBuildManifestFormat.Deserialize(IModSource source)
    {
    }
}
