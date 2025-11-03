using PackBuilder.Common.Project.IO;

namespace PackBuilder.Common.Project;

/// <summary>
///     Represents a mod project, containing metadata about a mod.
/// </summary>
/// <param name="Source">
///     The source view of this mod project, containing its files and structure.
/// </param>
/// <param name="Manifest">
///     Represents the build manifest, usually <c>build.txt</c>.
/// </param>
public readonly record struct ModProject(
    IModSource Source,
    BuildManifest Manifest
);
