using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Firearms;
internal class ViceEntity : BlockEntityDisplay
{
    private readonly InventoryGeneric _inventory;
    private readonly float[] _rotations = new float[4];

    public override string InventoryClassName => "vice";
    public override InventoryBase Inventory => _inventory;
    public override string AttributeTransformCode => "inViceTransform";

    public ViceEntity()
    {
        _inventory = new InventoryDisplayed(this, 1, "displaycase-0", null);
    }

    internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (slot.Empty)
        {
            if (TryTake(byPlayer, blockSel))
            {
                return true;
            }
            return false;
        }
        CollectibleObject colObj = slot.Itemstack.Collectible;
        if (colObj.Attributes != null && colObj.Attributes.KeyExists(AttributeTransformCode))
        {
            AssetLocation? sound = slot.Itemstack?.Block?.Sounds?.Place;
            if (TryPut(slot, blockSel, byPlayer))
            {
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
                return true;
            }
            return false;
        }
        (Api as ICoreClientAPI)?.TriggerIngameError(this, "doesnotfit", Lang.Get("This item does not fit into the vice."));
        return true;
    }

    private bool TryPut(ItemSlot slot, BlockSelection blockSel, IPlayer player)
    {
        int index = blockSel.SelectionBoxIndex;

        if (_inventory[index].Empty)
        {
            int num = slot.TryPutInto(Api.World, _inventory[index]);
            if (num > 0)
            {
                _rotations[index] = 0;
                MarkDirty();
            }
            return num > 0;
        }
        return false;
    }

    private bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (!_inventory[index].Empty)
        {
            ItemStack stack = _inventory[index].TakeOut(1);
            if (byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                AssetLocation? sound = stack.Block?.Sounds?.Place;
                Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
            }
            if (stack.StackSize > 0)
            {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            updateMesh(index);
            MarkDirty(redrawOnClient: true);
            return true;
        }
        return false;
    }

    protected override float[][] genTransformationMatrices()
    {
        float[][] tfMatrices = new float[4][];
        for (int index = 0; index < 4; index++)
        {
            float x = 0.5f;
            float y = 0.0f;
            float z = 0.5f;
            tfMatrices[index] = new Matrixf().Values;
        }
        return tfMatrices;
    }
}

internal class ViceBlock : BlockDisplayCase
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        return (world.BlockAccessor.GetBlockEntity(blockSel.Position) as ViceEntity)?.OnInteract(byPlayer, blockSel) ?? base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}
