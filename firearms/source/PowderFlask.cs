using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Firearms;

public class PowderFlaskStats
{
    public float DurabilityPerPowderItem { get; set; } = 8;
    public string PowderWildcard { get; set; } = "*gunpowder-fine";
    public string RefillSound { get; set; } = "maltiezfirearms:sounds/powder/powder-prime";
}

public class PowderFlask : Item
{
    public PowderFlaskStats Stats { get; set; } = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        Stats = Attributes.AsObject<PowderFlaskStats>();
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

        if (handling != EnumHandHandling.NotHandled) return;
        if (GetRemainingDurability(slot.Itemstack) == GetMaxDurability(slot.Itemstack)) return;

        handling = EnumHandHandling.Handled;
    }

    public override bool OnHeldInteractStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
    {
        base.OnHeldInteractStep(secondsPassed, slot, byEntity, blockSelection, entitySel);

        ItemSlot? powderSlot = null;
        byEntity.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item?.Code != null && WildcardUtil.Match(Stats.PowderWildcard, slot.Itemstack.Item.Code.ToString()))
            {
                powderSlot = slot;
                return false;
            }

            return true;
        });

        if (powderSlot?.Itemstack?.Item == null) return false;

        powderSlot.TakeOut(1);
        SetDurability(slot.Itemstack, (int)GameMath.Clamp(GetRemainingDurability(slot.Itemstack) + Stats.DurabilityPerPowderItem, 0, GetMaxDurability(slot.Itemstack)));

        powderSlot.MarkDirty();
        slot.MarkDirty();

        byEntity.Api.World.PlaySoundAt(new(Stats.RefillSound), atPlayer: (byEntity as EntityPlayer).Player, dualCallByPlayer: (byEntity as EntityPlayer).Player);

        return false;
    }
}
