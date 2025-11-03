using System;

namespace PackBuilder.Common.Project;

/// <summary>
///     A mod reference as used in the build manifest.
/// </summary>
/// <param name="ModName">The name of the mod.</param>
/// <param name="ModVersion">The optional target version of the mod.</param>
public readonly record struct ModReference(string ModName, string? ModVersion)
{
    public override string ToString()
    {
        return ModVersion is null ? ModName : $"{ModName}@{ModVersion}";
    }

    /// <summary>
    ///     Attempts to parse a mod reference of the format
    ///     <c>Name@Version</c> or <c>Name</c>.
    /// </summary>
    public static bool TryParse(string data, out ModReference modReference)
    {
        var split = data.Split('@', 2);

        if (split.Length == 1)
        {
            modReference = new ModReference(split[0], null);
            return true;
        }

        if (split.Length == 2 && Version.TryParse(split[1], out var version))
        {
            modReference = new ModReference(split[0], version.ToString());
            return true;
        }

        // If no parts or too many parts (somehow).
        modReference = default(ModReference);
        return false;
    }
}
