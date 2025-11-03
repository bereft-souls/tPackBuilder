using System.Threading.Tasks;
using PackBuilder.Common.Project.IO;
using PackBuilder.Common.Project.ManifestFormats;
using Terraria.ModLoader.Core;

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
)
{
    // TODO: Logging and what-not?
    public async Task<bool> Build()
    {
        var source = Source;
        // make sure manifest is written
        // TODO: Store manifest format on ModProject object when we abstract it.
        WellKnownBuildManifestFormats.BuildTxt.Serialize(Manifest, source);

        try
        {
            await Task.Run(
                () =>
                {
                    var compile = new ModCompile(new ModCompile.ConsoleBuildStatus());
                    compile.Build(source.GetDirectory().FullName);
                }
            );
            return true;
        }
        catch
        {
            return false;
        }
    }
}
