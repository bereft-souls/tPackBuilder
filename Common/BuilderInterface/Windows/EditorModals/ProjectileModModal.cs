using System.Collections.Generic;
using PackBuilder.Common.ModBuilding.Projectiles;
using PackBuilder.Common.ModBuilding.Projectiles.Changes;

namespace PackBuilder.Common.BuilderInterface.Windows.EditorModals;

internal sealed class ProjectileModModal : AbstractModal<ProjectileMod, ProjectileModModal.ProjectileModElement>
{
    public sealed class ProjectileModElement : ModifierModalElement<ProjectileMod>
    {
        private readonly SelectorProjectileWrapper projectilesList;

        public ProjectileModElement() : base("Projectile Mod", @static: true)
        {
            projectilesList = new SelectorProjectileWrapper([]);
            {
                projectilesList.Left.Set(0f, 0f);
                //npcsList.VAlign = 1f;
                projectilesList.Top.Set(38f, 0f);
                projectilesList.Width.Set(-2f, 0.5f);
                projectilesList.Height.Set(0f, 1f);
                projectilesList.List.Width.Set(0f, 1f);
                projectilesList.List.HAlign = 0f;
            }
            Append(projectilesList);
            MakeAndAppendLabel(projectilesList, "Projectiles");
        }

        public override void Recalculate()
        {
            base.Recalculate();

            var height = projectilesList.Top.Pixels + projectilesList.List.GetTotalHeight();
            Height.Set(height + PaddingTop + PaddingBottom, 0f);
        }

        public override ProjectileMod CreateObject()
        {
            var mod = new ProjectileMod();
            {
                mod.Projectiles.AddRange(projectilesList.Items);
            }
            return mod;
        }

        public override void Populate(ProjectileMod obj)
        {
            projectilesList.PopulateWithValues(obj.Projectiles);
        }
    }

    private sealed class VanillaProjectileChangeElement : ModifierModalElement<VanillaProjectileChange>, IVisitor<ProjectileMod>
    {
        private readonly InputField damageElement;
        private readonly InputField piercingElement;
        private readonly InputField scaleElement;
        private readonly InputField hitCooldownElement;

        public VanillaProjectileChangeElement() : base("Vanilla")
        {
            var offset = 42f;
            
            damageElement = new InputField(DefaultTextLol);
            {
                damageElement.Width.Set(90f, 0f);
                damageElement.Height.Set(20f, 0f);
            }
            
            piercingElement = new InputField(DefaultTextLol);
            {
                piercingElement.Width.Set(90f, 0f);
                piercingElement.Height.Set(20f, 0f);
            }
            
            scaleElement = new InputField(DefaultTextLol);
            {
                scaleElement.Width.Set(90f, 0f);
                scaleElement.Height.Set(20f, 0f);
            }
            
            hitCooldownElement = new InputField(DefaultTextLol);
            {
                hitCooldownElement.Width.Set(90f, 0f);
                hitCooldownElement.Height.Set(20f, 0f);
            }
            
            BuildFieldLine(ref offset, damageElement, piercingElement, scaleElement);
            {
                MakeAndAppendLabel(damageElement, "Damage");
                MakeAndAppendLabel(piercingElement, "Piercing");
                MakeAndAppendLabel(scaleElement, "Scale");
            }
            
            BuildFieldLine(ref offset, hitCooldownElement);
            {
                MakeAndAppendLabel(hitCooldownElement, "Hit Cooldown");
            }
            
            Height.Set(offset - damageElement.Height.Pixels / 2f, 0f);
        }

        public override VanillaProjectileChange CreateObject()
        {
            return new VanillaProjectileChange
            {
                Damage = damageElement.Text,
                Piercing = piercingElement.Text,
                Scale = scaleElement.Text,
                HitCooldown = hitCooldownElement.Text,
            };
        }

        public override void Populate(VanillaProjectileChange obj)
        {
            damageElement.Text = obj.Damage.ToStringOrEmpty();
            piercingElement.Text = obj.Piercing.ToStringOrEmpty();
            scaleElement.Text = obj.Scale.ToStringOrEmpty();
            hitCooldownElement.Text = obj.HitCooldown.ToStringOrEmpty();
        }

        public void Visit(ProjectileMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    public ProjectileModModal(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierModalElement> DeriveModifiers(ProjectileMod obj)
    {
        yield return CreateAndPopulate<ProjectileModElement, ProjectileMod>(obj);

        foreach (var change in obj.Changes)
        {
            if (change is VanillaProjectileChange vanilla)
            {
                yield return CreateAndPopulate<VanillaProjectileChangeElement, VanillaProjectileChange>(vanilla);
            }
        }
    }

    protected override IEnumerable<ModifierModalElement> GetAvailableModifiers()
    {
        yield return new VanillaProjectileChangeElement();
    }
}
