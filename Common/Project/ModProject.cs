using PackBuilder.Common.Project.IO;
using PackBuilder.Common.Project.ManifestFormats;
using System;
using System.Threading.Tasks;
using Terraria.ModLoader.Core;

namespace PackBuilder.Common.Project;

/// <summary>
///     An immutable view into a mod project.
/// </summary>
public readonly struct ModProjectView(ModProject project) : IEquatable<ModProjectView>, IComparable<ModProjectView>
{
    /// <summary>
    ///     Immutable view to relevant project properties.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    ///     The project has been deleted or disposed of.
    /// </exception>
    public ModProperties Properties => Project.Disposed
        ? throw new ObjectDisposedException("Cannot get properties of disposed project")
        : new ModProperties(Project.Manifest);

    public string InternalName => Project.InternalName;

    internal string Directory => Project.Directory;

    internal ModProject Project { get; } = project;

    public bool Equals(ModProjectView other)
    {
        return Project.Directory == other.Project.Directory;
    }

    public int CompareTo(ModProjectView other)
    {
        return string.Compare(Project.Directory, other.Project.Directory, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is ModProjectView other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Project.GetHashCode();
    }
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

    internal IModSource Source => source;

    public BuildManifest Manifest => manifest;

    public string InternalName => Source.GetDirectory().Name;

    public string Directory => Source.GetDirectory().FullName;

    // TODO: Logging and what-not?
    public async Task<bool> Build()
    {
        if (Disposed)
        {
            throw new ObjectDisposedException("Cannot build a disposed project");
        }

        // make sure manifest is written
        // TODO: Store manifest format on ModProject object when we abstract it.
        WellKnownBuildManifestFormats.BuildTxt.Serialize(manifest, Source);

        try
        {
            await Task.Run(
                () =>
                {
                    var compile = new ModCompile(new ModCompile.ConsoleBuildStatus());
                    compile.Build(Source.GetDirectory().FullName);
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
            Source.GetDirectory().Delete(recursive: true);
        }
        catch
        {
            // ignore
        }

        Dispose();
        return Source.GetDirectory().Exists;
    }

    public void Dispose()
    {
        Disposed = true;
    }
}
