using System.IO;

namespace PackBuilder.Common.Project.IO;

// TODO: Not finished, made early on to support potential use cases after.
// An eventual goal is to mask file access entirely and initialize a mod source
// with an opaque root and you manually navigate directories and files with in
// (blocking traversal above the root and access to irrelevant files, will
// simplify the browsing API too).

/// <summary>
///     Represents a mod source, a high level structure containing a mod's files
///     and layout.
///     <br />
///     A source may not necessarily expose all files and directories within a
///     mod, only those permitted to be made public for use.
/// </summary>
public interface IModSource
{
    // TODO: Look into exposing IO operations later.
    internal DirectoryInfo GetDirectory();
}

internal sealed class DirectoryModSource(string directory) : IModSource
{
    public DirectoryInfo GetDirectory()
    {
        return new DirectoryInfo(directory);
    }
}
