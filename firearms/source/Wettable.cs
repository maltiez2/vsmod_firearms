using CombatOverhaul.Inputs;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Firearms;

public class WettableStats
{
    public JsonItemStack TransitionStack { get; set; } = new();
    public string Sound { get; set; } = "";
}

public class Wettable : CollectibleBehavior, IOnInInventory
{
    public Wettable(CollectibleObject collObj) : base(collObj)
    {
    }

    public WettableStats Stats { get; set; } = new();

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        Stats = properties.AsObject<WettableStats>();
    }

    public void OnInInventory(EntityPlayer player, ItemSlot slot)
    {
        if (!player.Swimming) return;

        JsonItemStack stack = Stats.TransitionStack.Clone();
        if (!stack.Resolve(player.Api.World, "Wettable")) return;

        stack.ResolvedItemstack.Attributes.MergeTree(slot.Itemstack.Attributes);
        stack.ResolvedItemstack.StackSize = slot.Itemstack.StackSize;

        DummySlot dummySlot = new(stack.ResolvedItemstack);

        slot.TakeOutWhole();

        if (dummySlot.TryPutInto(player.Api.World, slot, stack.ResolvedItemstack.StackSize) <= 0) return;

        if (Stats.Sound != "")
        {
            player.Api.World.PlaySoundAt(new(Stats.Sound), player.Player, player.Player);
        }

        slot.MarkDirty();
    }
}