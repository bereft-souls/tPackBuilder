using System;
using System.Collections.Generic;
using System.Linq;

namespace PackBuilder.Common.Project.IO.Browsing;

public abstract class FileSystemEntry(
    string name,
    string fullPath,
    DirectoryEntry? parent
)
{
    public string Name { get; set; } = name;

    public string FullPath { get; set; } = fullPath;

    public DirectoryEntry? Parent { get; } = parent;
}

public sealed class DirectoryEntry(
    string name,
    string fullPath,
    DirectoryEntry? parent
) : FileSystemEntry(name, fullPath, parent)
{
    public List<DirectoryEntry> Directories { get; } = [];

    public List<FileEntry> Files { get; } = [];

    public IEnumerable<FileSystemEntry> Children => Directories.Cast<FileSystemEntry>().Concat(Files);

    public DirectoryEntry? GetDirectory(string name)
    {
        return Directories.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public FileEntry? GetFile(string name)
    {
        return Files.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class FileEntry(
    string name,
    string fullPath,
    DirectoryEntry? parent
) : FileSystemEntry(name, fullPath, parent);
