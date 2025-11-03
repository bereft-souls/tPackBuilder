using System.IO;

namespace PackBuilder.Common.Project.IO;

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
