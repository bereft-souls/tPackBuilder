using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PackBuilder.Core.Utils;

public static partial class ModUtils
{
    private static readonly Dictionary<Mod, BuildProperties> modProperties = [];
    extension(Mod mod)
    {
        public BuildProperties? buildProperties
        {
            get => modProperties.TryGetValue(mod, out var result) ? result : null;
            set
            {
                if (value is null)
                    modProperties.Remove(mod);

                else
                    modProperties[mod] = value;
            }
        }
    }

    private static readonly Dictionary<BuildProperties, BuildProperties.ModReference[]> tPBSoftReferences = [];
    extension(BuildProperties properties)
    {
        public BuildProperties.ModReference[]? packBuilderSoftRefs
        {
            get => tPBSoftReferences.TryGetValue(properties, out var result) ? result : null;
            set
            {
                if (value is null)
                    tPBSoftReferences.Remove(properties);

                else
                    tPBSoftReferences[properties] = value;
            }
        }
    }

    /// <summary>
    /// Splits a path to a given mod content file entry into its respective mod name and content name.
    /// </summary>
    public static void SplitModContent(string modContent, out string mod, out string content)
    {
        var split = modContent.Split('/');
        mod = split[0];
        content = split[1];
    }

    /// <summary>
    /// Checks whether the supplied mod is both unloaded and listed as a "soft mod" in the registering mod.
    /// </summary>
    public static bool SoftReferenceUnloaded(this Mod? registeringMod, string softMod)
    {
        if (registeringMod is null)
            return false;

        registeringMod.buildProperties ??= BuildProperties.ReadModFile(registeringMod.File);
        var properties = registeringMod.buildProperties;

        if (properties is null)
            return false;

        return properties.packBuilderSoftRefs?.Any(r => r.mod == softMod) ?? false && !ModLoader.IsEnabled(softMod);
    }

    /// <summary>
    /// Gets the ID for an npc based on its content path, accounting for both vanilla and modded entries.
    /// </summary>
    public static int GetNPC(string npc, Mod? registeringMod = null)
    {
        SplitModContent(npc, out var mod, out var name);

        try
        {
            if (mod == "Terraria")
                return (short)(typeof(NPCID).GetField(name)?.GetRawConstantValue() ?? throw new Exception());

            if (registeringMod.SoftReferenceUnloaded(mod))
                return NPCID.None;

            return ModContent.Find<ModNPC>(mod, name).Type;
        }
        catch
        {
            throw new HideStackTraceException($"NPC type \"{npc}\" not found!");
        }
    }

    /// <summary>
    /// Gets the ID for an item based on its content path, accounting for both vanilla and modded entries.
    /// </summary>
    public static int GetItem(string item, Mod? registeringMod = null)
    {
        SplitModContent(item, out var mod, out var name);

        try
        {
            if (mod == "Terraria")
                return (short)(typeof(ItemID).GetField(name)?.GetRawConstantValue() ?? throw new Exception());

            if (registeringMod.SoftReferenceUnloaded(mod))
                return ItemID.None;

            return ModContent.Find<ModItem>(mod, name).Type;
        }
        catch
        {
            throw new HideStackTraceException($"Item type \"{item}\" not found!");
        }
    }

    /// <summary>
    /// Gets the ID for a projectile based on its content path, accounting for both vanilla and modded entries.
    /// </summary>
    /// <param name="projectile"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static int GetProjectile(string projectile, Mod? registeringMod = null)
    {
        SplitModContent(projectile, out var mod, out var name);

        try
        {
            if (mod == "Terraria")
                return (short)(typeof(ProjectileID).GetField(name)?.GetRawConstantValue() ?? throw new Exception());

            if (registeringMod.SoftReferenceUnloaded(mod))
                return ProjectileID.None;

            return ModContent.Find<ModProjectile>(mod, name).Type;
        }
        catch
        {
            throw new HideStackTraceException($"Projectile type \"{projectile}\" not found!");
        }
    }

    /// <summary>
    /// Gets the ID for a tile based on its content path, accounting for both vanilla and modded entries.
    /// </summary>
    public static int GetTile(string tile, Mod? registeringMod = null)
    {
        SplitModContent(tile, out var mod, out var name);

        try
        {
            if (mod == "Terraria")
                return (ushort)(typeof(TileID).GetField(name)?.GetRawConstantValue() ?? throw new Exception());

            if (registeringMod.SoftReferenceUnloaded(mod))
                return -1;

            return ModContent.Find<ModTile>(mod, name).Type;
        }
        catch
        {
            throw new HideStackTraceException($"Tyle type \"{tile}\" not found!");
        }
    }

    /// <summary>
    /// Gets the ID for a recipe group based on its specified name.
    /// </summary>
    public static int GetRecipeGroup(string group)
    {
        if (!RecipeGroup.recipeGroupIDs.TryGetValue(group, out int id))
            throw new HideStackTraceException($"Recipe group \"{group}\" not found!");

        return id;
    }
    
    public static bool FuzzyMatch(string? text, string? query)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (string.IsNullOrEmpty(query))
        {
            return true;
        }

        var queryIdx = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (char.ToLowerInvariant(text[i]) == char.ToLowerInvariant(query[queryIdx]))
            {
                queryIdx++;

                if (queryIdx == query.Length)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Creates a recipe from the specified mod.
    /// </summary>
    public static Recipe NewRecipe(Mod mod) => (Recipe)recipeConstructor.Invoke([mod]);
    private static readonly ConstructorInfo recipeConstructor = typeof(Recipe).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, [typeof(Mod)])!;

    public static void SetDisabled(this Recipe recipe, bool disabled) => disabledProperty.SetValue(recipe, disabled);
    private static readonly PropertyInfo disabledProperty = typeof(Recipe).GetProperty("Disabled", BindingFlags.Public | BindingFlags.Instance)!;
}