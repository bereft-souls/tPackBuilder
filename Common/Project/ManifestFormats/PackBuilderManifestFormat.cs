using PackBuilder.Common.Project.IO;
using ReLogic.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackBuilder.Common.Project.ManifestFormats;

/// <summary>
///     Implements the <c>packbuilder.txt</c> format.
/// </summary>
internal sealed class PackBuilderManifestFormat : IBuildManifestFormat
{
    void IBuildManifestFormat.Serialize(BuildManifest manifest, IModSource source)
    {
        var sb = new StringBuilder();

        if (manifest.SoftModReferences.Count > 0)
        {
            WriteList(sb, "softReferences", manifest.SoftModReferences);
        }

        var dir = source.GetDirectory().FullName;
        if (sb.Length != 0)
        {
            File.WriteAllText(Path.Combine(dir, "packbuilder.txt"), sb.ToString());
        }

        return;

        static void WriteList<T>(StringBuilder sb, string property, IEnumerable<T> values)
        {
            sb.AppendLine($"{property} = {string.Join(',', values)}");
        }
    }

    bool IBuildManifestFormat.Deserialize(BuildManifest manifest, IModSource source)
    {
        var dir = source.GetDirectory();
        var packBuilderTxtPath = Path.Combine(dir.FullName, "packbuilder.txt");

        if (!File.Exists(packBuilderTxtPath))
        {
            // It's okay for this one to be missing...
            return true;
        }

        foreach (var line in File.ReadAllLines(packBuilderTxtPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var split = line.Split('=', 2);
            if (split.Length != 2)
                continue;

            var property = split[0].Trim();
            var value = split[1].Trim();
            if (value.Length == 0)
                continue;

            switch (property)
            {
                case "softReferences":
                    var softRefs = new List<ModReference>();
                    foreach (var modRefVal in ReadList(value))
                    {
                        if (ModReference.TryParse(modRefVal, out var modRef))
                            softRefs.Add(modRef);
                    }

                    manifest.SoftModReferences.AddRange(softRefs);
                    break;
            }
        }

        return true;

        static IEnumerable<string> ReadList(string value)
        {
            return value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0);
        }
    }
}
