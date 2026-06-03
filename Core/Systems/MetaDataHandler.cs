using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PackBuilder.Core.Systems
{
    internal sealed class MetaDataHandler : ModSystem
    {
        public override void Load()
        {
            var hook = typeof(BuildProperties).GetMethod(nameof(BuildProperties.ReadModFile), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(TmodFile)]);
            MonoModHooks.Add(hook, ReadPackBuilderMetadata);
        }

        // We use this to automatically read additonal metadata from an optional "packbuilder.txt" file
        // If build.txt serialization changes to allow for custom metadata that would be great but this works for now
        public static BuildProperties ReadPackBuilderMetadata(Func<TmodFile, BuildProperties> orig, TmodFile modFile)
        {
            BuildProperties properties = orig(modFile);

            if (!modFile.files.TryGetValue("packbuilder.txt", out var packBuilderMetaFile))
                return properties;

            string build = Encoding.UTF8.GetString(modFile.GetBytes(packBuilderMetaFile));
            foreach (string line in build.Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int split = line.IndexOf('=');
                if (split < 0)
                    continue; // lines without an '=' are ignored

                string property = line[..split].Trim();
                string value = line[(split + 1)..].Trim();

                switch (property)
                {
                    case "softReferences":
                        properties.packBuilderSoftRefs = [..BuildProperties.ReadList(value).Select(BuildProperties.ModReference.Parse)];
                        break;
                }
            }

            return properties;
        }
    }
}
