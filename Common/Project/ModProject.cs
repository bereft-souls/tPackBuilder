using System;
using System.Threading.Tasks;
using PackBuilder.Common.Project.IO;
using PackBuilder.Common.Project.ManifestFormats;
using Terraria.ModLoader.Core;

namespace PackBuilder.Common.Project;

/// <summary>
///     An immutable view into a mod project.
/// </summary>
public readonly struct ModProjectView(ModProject project)
{
    /// <summary>
    ///     Immutable view to relevant project properties.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///     The project has been deleted or disposed of.
    /// </exception>
    public ModProperties Properties => project.Disposed
        ? throw new ObjectDisposedException("Cannot get properties of disposed project")
        : new ModProperties(project.Manifest);
}

/// <summary>
///     Represents a mod project, containing metadata about a mod.
/// </summary>
public sealed class ModProject(
    IModSource source,
    BuildManifest manifest
) : IDisposable
{
    public bool Disposed { get; private set; }

    public BuildManifest Manifest => manifest;

    // TODO: Logging and what-not?
    public async Task<bool> Build()
    {
        if (Disposed)
        {
            throw new ObjectDisposedException("Cannot build a disposed project");
        }

        // make sure manifest is written
        // TODO: Store manifest format on ModProject object when we abstract it.
        WellKnownBuildManifestFormats.BuildTxt.Serialize(manifest, source);

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

    /// <summary>
    ///     Deletes this project, disposing of it automatically in the process.
    /// </summary>
    public bool Delete()
    {
        try
        {
            source.GetDirectory().Delete(recursive: true);
        }
        catch
        {
            // ignore
        }

        Dispose();
        return source.GetDirectory().Exists;
    }

    public void Dispose()
    {
        Disposed = true;
    }
}
