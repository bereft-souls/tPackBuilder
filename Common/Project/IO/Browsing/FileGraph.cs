using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PackBuilder.Common.Project.IO.Browsing;

public sealed class FileGraph : IDisposable
{
    public DirectoryEntry Root { get; }

    private readonly FileSystemWatcher watcher;
    private readonly HashSet<string> allowedExtensions;

    public event Action<FileEntry>? FileAdded;
    public event Action<FileEntry>? FileRemoved;
    public event Action<FileEntry>? FileChanged;
    public event Action<DirectoryEntry>? DirectoryAdded;
    public event Action<DirectoryEntry>? DirectoryRemoved;

    // allowedExtensions needs to support compound ones like .itemmod.json
    public FileGraph(string rootPath, params string[] allowedExtensions)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Cannot create watched file graph for directory: {rootPath}");

        this.allowedExtensions = allowedExtensions
                                .Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : '.' + x.ToLowerInvariant())
                                .ToHashSet();

        Root = BuildRecursive(rootPath);

        watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };

        watcher.Created += OnCreated;
        watcher.Deleted += OnDeleted;
        watcher.Changed += OnChanged;
        watcher.Renamed += OnRenamed;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        if (Directory.Exists(e.FullPath))
        {
            var parent = FindParentDirectory(e.FullPath);
            if (parent is null)
            {
                return;
            }

            var newDir = new DirectoryEntry(Path.GetFileName(e.FullPath), e.FullPath, parent);
            parent.Directories.Add(newDir);
            DirectoryAdded?.Invoke(newDir);
        }
        else if (File.Exists(e.FullPath))
        {
            var ext = GetComplicatedExtension(e.FullPath, allowedExtensions).ToLowerInvariant();
            if (allowedExtensions.Count != 0 && !allowedExtensions.Contains(ext))
            {
                return;
            }

            var parent = FindParentDirectory(e.FullPath);
            if (parent is null)
            {
                return;
            }

            var newFile = new FileEntry(Path.GetFileName(e.FullPath), e.FullPath, parent);
            parent.Files.Add(newFile);
            FileAdded?.Invoke(newFile);
        }
    }

    private void OnDeleted(object sender, FileSystemEventArgs e)
    {
        var file = FindFile(e.FullPath);
        if (file is not null)
        {
            file.Parent?.Files.Remove(file);
            FileRemoved?.Invoke(file);
            return;
        }

        var dir = FindDirectory(e.FullPath);
        if (dir is not null)
        {
            dir.Parent?.Directories.Remove(dir);
            DirectoryRemoved?.Invoke(dir);
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var file = FindFile(e.FullPath);
        if (file is not null)
            FileChanged?.Invoke(file);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        var entry = FindFile(e.OldFullPath) as FileSystemEntry ?? FindDirectory(e.OldFullPath);
        if (entry is null)
        {
            return;
        }

        entry.Name = Path.GetFileName(e.FullPath);
        entry.FullPath = e.FullPath;
    }

    private DirectoryEntry BuildRecursive(string path, DirectoryEntry? parent = null)
    {
        var dir = new DirectoryEntry(Path.GetFileName(path), path, parent);

        foreach (var subDir in Directory.GetDirectories(path))
            dir.Directories.Add(BuildRecursive(subDir, dir));

        foreach (var file in Directory.GetFiles(path))
        {
            var ext = GetComplicatedExtension(path, allowedExtensions).ToLowerInvariant();

            // 0 means no whitelist, all are allowed.
            if (allowedExtensions.Count == 0 || allowedExtensions.Contains(ext))
                dir.Files.Add(new FileEntry(Path.GetFileName(file), file, dir));
        }

        return dir;
    }

    public FileEntry CreateFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(Root.FullPath, relativePath);
        var dirPath = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dirPath);
        File.WriteAllText(fullPath, content);

        return FindFile(fullPath)!;
    }

    public void DeleteFile(string relativePath)
    {
        var fullPath = Path.Combine(Root.FullPath, relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public void EditFile(string relativePath, string newContent)
    {
        var fullPath = Path.Combine(Root.FullPath, relativePath);
        if (File.Exists(fullPath))
            File.WriteAllText(relativePath, newContent);
    }

    public DirectoryEntry CreateDirectory(string relativePath)
    {
        var fullPath = Path.Combine(Root.FullPath, relativePath);
        Directory.CreateDirectory(fullPath);
        return FindDirectory(fullPath)!;
    }

    public void DeleteDirectory(string relativePath)
    {
        var fullPath = Path.Combine(Root.FullPath, relativePath);
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, true);
    }

    public DirectoryEntry? FindParentDirectory(string fullPath)
    {
        var parentPath = Path.GetDirectoryName(fullPath);
        return parentPath is null ? null : FindDirectory(parentPath);
    }

    public DirectoryEntry? FindDirectory(string fullPath)
    {
        if (string.Equals(fullPath, Root.FullPath, StringComparison.OrdinalIgnoreCase))
            return Root;

        var parts = Path.GetRelativePath(Root.FullPath, fullPath)
                        .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        var current = Root;
        foreach (var part in parts)
        {
            var next = current.GetDirectory(part);
            if (next is null)
                return null;

            current = next;
        }

        return current;
    }

    public FileEntry? FindFile(string fullPath)
    {
        var dir = FindParentDirectory(fullPath);
        return dir?.GetFile(Path.GetFileName(fullPath));
    }

    public void Dispose()
    {
        watcher.Dispose();
    }

    private static string GetComplicatedExtension(string path, HashSet<string>? extensions)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        var fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;

        if (extensions is null || extensions.Count == 0)
            return Path.GetExtension(fileName).ToLowerInvariant();

        var lowerName = fileName.ToLowerInvariant();

        var bestMatch = default(string?);
        foreach (var extension in extensions)
        {
            if (string.IsNullOrWhiteSpace(extension))
                continue;

            var lowerExt = extension.ToLowerInvariant();
            if (!lowerName.EndsWith(lowerExt, StringComparison.OrdinalIgnoreCase))
                continue;

            // Prefer the longest extension (.itemmod.json beats .json).
            if (bestMatch is null || lowerExt.Length > bestMatch.Length)
                bestMatch = lowerExt;
        }

        return bestMatch ?? Path.GetExtension(fileName).ToLowerInvariant();
    }
}
