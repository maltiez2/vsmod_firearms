using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Firearms;

public class IgniteableStats
{
    public JsonItemStack TransitionStack { get; set; } = new();
    public string Sound { get; set; } = "";
    public string[] IgniteFrom { get; set; } = Array.Empty<string>();
}

public class Igniteable : CollectibleBehavior
{
    public Igniteable(CollectibleObject collObj) : base(collObj)
    {
    }

    public IgniteableStats Stats { get; set; } = new();

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        Stats = properties.AsObject<IgniteableStats>();
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        byEntity.Api.Logger.Notification($"{byEntity.Api.Side} - blockSel: {blockSel?.Block != null}");

        if (byEntity.Api.Side == EnumAppSide.Server)
        {
            //byEntity.Api.Logger.Notification($"{byEntity.Api.Side}");
        }

        if (blockSel?.Block == null) return;
        string code = blockSel.Block.Code.ToShortString();

        bool satisfy = false;
        foreach (string wildcard in Stats.IgniteFrom)
        {
            if (WildcardUtil.Match(wildcard, code))
            {
                satisfy = true;
                break;
            }
        }

        if (!satisfy) return;

        if (byEntity is EntityPlayer player)
        {
            RepalceStack(slot, player);
            handling = EnumHandling.Handled;
            handHandling = EnumHandHandling.Handled;

            byEntity.Api.Logger.Notification($"{byEntity.Api.Side}");
        }
    }

    private void RepalceStack(ItemSlot slot, EntityPlayer player)
    {
        JsonItemStack stack = Stats.TransitionStack.Clone();

        if (!stack.Resolve(player.Api.World, "Igniteable")) return;

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