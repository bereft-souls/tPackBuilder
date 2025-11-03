using System.IO;

namespace PackBuilder.Common.Project.IO;

internal sealed class DirectoryModSource(string directory) : IModSource
{
    public DirectoryInfo GetDirectory()
    {
        return new DirectoryInfo(directory);
    }
}
