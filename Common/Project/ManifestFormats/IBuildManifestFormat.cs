using PackBuilder.Common.Project.IO;

namespace PackBuilder.Common.Project.ManifestFormats;

// TODO: See about providing file names and auto-detection.

/// <summary>
///     A format for reading and writing <see cref="BuildManifest"/>s.
/// </summary>
public interface IBuildManifestFormat
{
    // TODO: If merged with tml-build structure, emit diagnostics.
    /// <summary>
    ///     Serializes the <paramref name="manifest"/> to the given
    ///     <paramref name="source"/>.
    /// </summary>
    void Serialize(BuildManifest manifest, IModSource source);

    // TODO: If merged with tml-build structure, emit diagnostics.
    /// <summary>
    ///     Attempts to deserialize a manifest from the given
    ///     <paramref name="source"/>, returning <see langword="null"/> if the
    ///     prerequisite file is not found or bad data was given.
    /// </summary>
    BuildManifest? Deserialize(IModSource source);
}
