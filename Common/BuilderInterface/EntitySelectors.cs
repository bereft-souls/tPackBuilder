using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

internal abstract class BaseSelector<TEntry, TEntity> : UIElement
    where TEntry : GridEntry
{
    public TEntity Entity { get; set; }

    protected BaseSelector()
    {
        Width.Set(150f, 0f);
        Height.Set(24f, 0f);

        SearchGrid = new ElementSearchGrid<TEntry>(5, 7, BuilderInterfaceSystem.State);
        SearchGrid.FitsFilter += FitsFilter;
        SearchGrid.OnSubmit += OnSubmit;
        SearchGrid.Populate(GetEntries());

        Text = new InputField(string.Empty);
        Text.TextScale = 0.8f;
        Text.Width.Set(0f, 1f);
        Text.Height.Set(0f, 1f);
        Append(Text);

        var searchButton = new UIImageButton(ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/SearchSmall", AssetRequestMode.ImmediateLoad));
        searchButton.SetHoverImage(ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/HoverOutlineSmall", AssetRequestMode.ImmediateLoad));
        searchButton.HAlign = 1f;
        searchButton.VAlign = 0.5f;
        searchButton.OnLeftClick += OpenSelector;
        Append(searchButton);
    }

    private void OnSubmit()
    {
        Text.PressEnter();
    }

    public ElementSearchGrid<TEntry> SearchGrid { get; }

    public InputField Text { get; }

    protected abstract List<TEntry> GetEntries();

    public override void Recalculate()
    {
        base.Recalculate();

        var dims = GetDimensions();

        SearchGrid.Left.Set(dims.X + dims.Width + 4f, 0f);
        SearchGrid.Top.Set(dims.Y, 0f);
    }

    protected abstract bool FitsFilter(TEntry entry, string query);

    protected void OpenSelector(UIMouseEvent evt, UIElement listeningElement)
    {
        BuilderInterfaceSystem.State.AddWindowOrBringToFront(SearchGrid);
        SearchGrid.BringToFront();
    }
}

internal sealed class ItemTypeSelector : BaseSelector<ItemTypeSelector.ItemTypeEntry, string>
{
    public sealed class ItemTypeEntry(int type) : GridEntry
    {
        public int ItemType { get; } = type;

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!IsMouseHovering)
            {
                return;
            }

            Main.HoverItem = ContentSamples.ItemsByType[ItemType];
            Main.instance.MouseText("", 0, 0);
            Main.mouseText = true;
        }

        public override void DrawEntry(UIElement affectedElement)
        {
            if (!TextureAssets.Item[ItemType].IsLoaded)
            {
                Main.instance.LoadItem(ItemType);
            }

            var deprecated = ItemID.Sets.Deprecated[ItemType];
            var center = GetDimensions().Center();

            ItemID.Sets.Deprecated[ItemType] = false;
            try
            {
                var item = ContentSamples.ItemsByType[ItemType];
                if (deprecated)
                {
                    item = new Item(ItemType);
                }

                ItemSlot.DrawItemIcon(item, 31, Main.spriteBatch, center, 1f, 24f, Color.White);
            }
            finally
            {
                ItemID.Sets.Deprecated[ItemType] = deprecated;
            }
        }
    }

    public ItemTypeSelector()
    {
        Entity = "None";
        Text.Text = Entity;
    }

    protected override List<ItemTypeEntry> GetEntries()
    {
        var entries = new List<ItemTypeEntry>();
        for (var i = 0; i < ItemLoader.ItemCount; i++)
        {
            var entry = new ItemTypeEntry(i);
            entry.OnLeftClick += PickItem;
            entries.Add(entry);
        }

        return entries;
    }

    private void PickItem(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is ItemTypeEntry entry)
        {
            Entity = ItemID.Search.GetName(entry.ItemType);
            Text.Text = Entity;
            SearchGrid.Submit();
        }

        SearchGrid.Close();
    }

    protected override bool FitsFilter(ItemTypeEntry entry, string query)
    {
        if (int.TryParse(query, out var possibleId) && possibleId >= ItemID.None && possibleId < ItemLoader.ItemCount)
        {
            return entry.ItemType == possibleId;
        }

        return FuzzyMatch(ItemID.Search.GetName(entry.ItemType), query)
            || FuzzyMatch(Lang.GetItemNameValue(entry.ItemType), query);
    }
}

internal sealed class NpcTypeSelector : BaseSelector<NpcTypeSelector.NpcTypeEntry, string>
{
    public sealed class NpcTypeEntry : GridEntry
    {
        private readonly BestiaryEntry entry;
        private readonly UnlockableNPCEntryIcon icon;

        private readonly NPC npc;

        public NpcTypeEntry(int netId)
        {
            NpcNetId = netId;
            npc = ContentSamples.NpcsByNetId[netId];
            icon = new UnlockableNPCEntryIcon(netId);
            entry = Main.BestiaryDB.FindEntryByNPCID(netId);

            OverrideSamplerState = SamplerState.PointClamp;
            UseImmediateMode = true;
        }

        public int NpcNetId { get; }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!IsMouseHovering)
            {
                return;
            }

            var id = NPCID.FromNetId(NpcNetId);
            var mod = NPCLoader.GetNPC(id)?.Mod.Name ?? "Terraria";
            var name = NPCID.Search.GetName(id);
            var display = Lang.GetNPCName(NpcNetId);
            UICommon.TooltipMouseText($"{display} ({mod}/{name})");
        }

        public override void DrawEntry(UIElement affectedElement)
        {
            base.DrawEntry(affectedElement);

            var info = new BestiaryUICollectionInfo
            {
                UnlockState = BestiaryEntryUnlockState.CanShowPortraitOnly_1,
            };

            var settings = new EntryIconDrawSettings
            {
                iconbox = GetDimensions().ToRectangle(),
                IsPortrait = true,
            };

            var clip = GetDimensions().ToRectangle();
            clip.Inflate(-1, -1);
            clip.Offset(1, 1);
            var infAmount = (int)(clip.Width * Main.UIScale) - clip.Width;
            clip.Inflate(infAmount / 2, infAmount / 2);
            var offset = (clip.Center() * Main.UIScale - clip.Center()).ToPoint();
            clip.Offset(offset);

            var scissorRectangle = Main.graphics.GraphicsDevice.ScissorRectangle;
            var isABestiaryIconDummy = npc.IsABestiaryIconDummy;
            try
            {
                Main.graphics.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(clip, scissorRectangle);
                npc.IsABestiaryIconDummy = true;
                icon.Draw(info, Main.spriteBatch, settings);
            }
            catch
            {
                // ignore
            }
            finally
            {
                npc.IsABestiaryIconDummy = isABestiaryIconDummy;
                Main.graphics.GraphicsDevice.ScissorRectangle = scissorRectangle;
            }
        }
    }

    public NpcTypeSelector()
    {
        Entity = "None";
        Text.Text = Entity;

        OverrideSamplerState = SamplerState.PointClamp;
        UseImmediateMode = true;
    }

    protected override List<NpcTypeEntry> GetEntries()
    {
        var entries = new List<NpcTypeEntry>();
        for (var i = NPCID.None; i < NPCLoader.NPCCount; i++)
        {
            var entry = new NpcTypeEntry(i);
            entry.OnLeftClick += PickNpc;
            entries.Add(entry);
        }

        return entries;
    }

    private void PickNpc(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is NpcTypeEntry entry)
        {
            if (entry.NpcNetId >= 0)
            {
                Entity = NPCID.Search.GetName(entry.NpcNetId);
            }
            else
            {
                Entity = NPCID.Search.GetName(NPCID.FromNetId(entry.NpcNetId));
            }

            Text.Text = Entity;
            SearchGrid.Submit();
        }

        SearchGrid.Close();
    }

    protected override bool FitsFilter(NpcTypeEntry entry, string query)
    {
        if (int.TryParse(query, out var possibleId) && possibleId >= NPCID.None && possibleId < NPCLoader.NPCCount)
        {
            return entry.NpcNetId == possibleId;
        }

        string name;
        if (entry.NpcNetId >= 0)
        {
            name = NPCID.Search.GetName(entry.NpcNetId);
        }
        else
        {
            name = NPCID.Search.GetName(NPCID.FromNetId(entry.NpcNetId));
        }

        return FuzzyMatch(name, query)
            || FuzzyMatch(Lang.GetNPCNameValue(entry.NpcNetId), query);
    }
}

internal sealed class ProjectileTypeSelector : BaseSelector<ProjectileTypeSelector.ProjectileTypeEntry, string>
{
    public sealed class ProjectileTypeEntry(int type) : GridEntry
    {
        public int ProjectileType { get; } = type;

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!IsMouseHovering)
            {
                return;
            }

            var mod = ProjectileLoader.GetProjectile(ProjectileType)?.Mod.Name ?? "Terraria";
            var name = ProjectileID.Search.GetName(ProjectileType);
            var display = Lang.GetProjectileName(ProjectileType);
            UICommon.TooltipMouseText($"{display} ({mod}/{name})");
        }

        public override void DrawEntry(UIElement affectedElement)
        {
            base.DrawEntry(affectedElement);

            Main.instance.LoadProjectile(ProjectileType);
            var texture = TextureAssets.Projectile[ProjectileType].Value;
            var frame = texture.Frame(verticalFrames: Main.projFrames[ProjectileType]);
            if (texture.Width > texture.Height)
            {
                frame = texture.Frame(horizontalFrames: Main.projFrames[ProjectileType]);
            }

            var scale = 1f;
            if (frame.Width > 24 || frame.Height > 24)
            {
                scale = 24f / Math.Max(frame.Width, frame.Height);
            }

            Main.spriteBatch.Draw(texture, GetDimensions().Center() + Vector2.One, frame, Color.White, 0f, frame.Size() / 2f, scale, SpriteEffects.None, 0f);
        }
    }

    public ProjectileTypeSelector()
    {
        Entity = "None";
        Text.Text = Entity;
    }

    protected override List<ProjectileTypeEntry> GetEntries()
    {
        var entries = new List<ProjectileTypeEntry>();
        for (var i = 0; i < ProjectileLoader.ProjectileCount; i++)
        {
            var entry = new ProjectileTypeEntry(i);
            entry.OnLeftClick += PickProj;
            entries.Add(entry);
        }

        return entries;
    }

    private void PickProj(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is ProjectileTypeEntry entry)
        {
            Entity = ProjectileID.Search.GetName(entry.ProjectileType);
            Text.Text = Entity;
            SearchGrid.Submit();
        }

        SearchGrid.Close();
    }

    protected override bool FitsFilter(ProjectileTypeEntry entry, string query)
    {
        if (int.TryParse(query, out var possibleId) && possibleId >= ProjectileID.None && possibleId < ProjectileLoader.ProjectileCount)
        {
            return entry.ProjectileType == possibleId;
        }

        return FuzzyMatch(ProjectileID.Search.GetName(entry.ProjectileType), query)
            || FuzzyMatch(Lang.GetProjectileName(entry.ProjectileType).Value, query);
    }
}

internal sealed class RecipeGroupSelector : BaseSelector<RecipeGroupSelector.RecipeGroupEntry, string>
{
    public sealed class RecipeGroupEntry(string name) : GridEntry
    {
        public string Name { get; } = name;

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!IsMouseHovering)
            {
                return;
            }

            var recipeGroup = RecipeGroup.recipeGroups[RecipeGroup.recipeGroupIDs[Name]];
            UICommon.TooltipMouseText($"{recipeGroup.GetText()} ({Name})");
        }

        public override void DrawEntry(UIElement affectedElement)
        {
            base.DrawEntry(affectedElement);

            if (Name == "None")
            {
                return;
            }

            var recipeGroup = RecipeGroup.recipeGroups[RecipeGroup.recipeGroupIDs[Name]];
            var items = recipeGroup.ValidItems.ToArray();

            var idx = (int)(Main.GlobalTimeWrappedHourly % items.Length);

            var item = items[idx];
            DrawItem(item);
        }

        private void DrawItem(int itemType)
        {
            if (!TextureAssets.Item[itemType].IsLoaded)
            {
                Main.instance.LoadItem(itemType);
            }

            var deprecated = ItemID.Sets.Deprecated[itemType];
            var center = GetDimensions().Center();

            ItemID.Sets.Deprecated[itemType] = false;
            try
            {
                var item = ContentSamples.ItemsByType[itemType];
                if (deprecated)
                {
                    item = new Item(itemType);
                }

                ItemSlot.DrawItemIcon(item, 31, Main.spriteBatch, center, 1f, 24f, Color.White);
            }
            finally
            {
                ItemID.Sets.Deprecated[itemType] = deprecated;
            }
        }
    }

    public RecipeGroupSelector()
    {
        Entity = "None";
        Text.Text = Entity;
    }

    protected override List<RecipeGroupEntry> GetEntries()
    {
        var entries = new List<RecipeGroupEntry>();
        foreach (var name in new[] { "None" }.Concat(RecipeGroup.recipeGroupIDs.Keys))
        {
            // var recipeGroup = RecipeGroup.recipeGroups[id];
            var entry = new RecipeGroupEntry(name);
            entry.OnLeftClick += PickGroup;
            entries.Add(entry);
        }

        return entries;
    }

    private void PickGroup(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is RecipeGroupEntry entry)
        {
            Entity = entry.Name;
            Text.Text = Entity;
            SearchGrid.Submit();
        }

        SearchGrid.Close();
    }

    protected override bool FitsFilter(RecipeGroupEntry entry, string query)
    {
        return FuzzyMatch(entry.Name, query);
    }
}

internal sealed class TileTypeSelector : BaseSelector<TileTypeSelector.TileTypeEntry, string>
{
    public sealed class TileTypeEntry(int type) : GridEntry
    {
        public int TileType { get; } = type;

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!IsMouseHovering)
            {
                return;
            }


            var mod = TileLoader.GetTile(TileType)?.Mod.Name ?? "Terraria";
            var name = TileID.Search.GetName(TileType);
            UICommon.TooltipMouseText($"{mod}/{name}");
        }

        public override void DrawEntry(UIElement affectedElement)
        {
            base.DrawEntry(affectedElement);

            if (TileType < 0)
            {
                return;
            }

            Main.instance.LoadTiles(TileType);
            var texture = TextureAssets.Tile[TileType].Value;
            // var tileData = TileObjectData.GetTileData(TileType, style: 0);

            Main.spriteBatch.Draw(texture, GetDimensions().Center() + Vector2.One, new Rectangle(0, 0, 16, 16), Color.White, 0f, new Vector2(8f), 1f, SpriteEffects.None, 0f);
        }
    }

    public TileTypeSelector()
    {
        Entity = "None";
        Text.Text = Entity;
    }

    protected override List<TileTypeEntry> GetEntries()
    {
        var entries = new List<TileTypeEntry>();
        for (var i = -1; i < TileLoader.TileCount; i++)
        {
            var entry = new TileTypeEntry(i);
            entry.OnLeftClick += PickTile;
            entries.Add(entry);
        }

        return entries;
    }

    private void PickTile(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is TileTypeEntry entry)
        {
            Entity = entry.TileType < 0 ? "None" : TileID.Search.GetName(entry.TileType);
            Text.Text = Entity;
            SearchGrid.Submit();
        }

        SearchGrid.Close();
    }

    protected override bool FitsFilter(TileTypeEntry entry, string query)
    {
        if (int.TryParse(query, out var possibleId) && possibleId >= TileID.Dirt && possibleId < TileLoader.TileCount)
        {
            return entry.TileType == possibleId;
        }

        return FuzzyMatch(TileID.Search.GetName(entry.TileType), query);
        //|| FuzzyMatch(Lang.GetTi(entry.TileType).Value, query);
    }
}