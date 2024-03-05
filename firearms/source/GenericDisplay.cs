using System;
using System.Linq;
using System.Security.Cryptography;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Firearms;

public class GenericDisplayBlockEntity : BlockEntityDisplay
{
    private InventoryGeneric _inventory;
    private string _inventoryTranformName = "inGenericDisplayTransform";
    private float[][]? _transforms;

    public override string InventoryClassName => "vice";
    public override InventoryBase Inventory => _inventory;
    public override string AttributeTransformCode => _inventoryTranformName;

    public const int MaxSlots = 16;

    public GenericDisplayBlockEntity()
    {
        _inventory = new InventoryDisplayed(this, MaxSlots, "display-generic", null);
    }

    public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
    {
        base.CreateBehaviors(block, worldForResolve);

        if (block.Attributes == null) return;

        int size = block.Attributes["inventorySize"].AsInt(1);
        string transform = block.Attributes["inventoryTransformAttribute"].AsString("inGenericDisplayTransform");
        float rotateX = block.Attributes["rotate"]?["x"]?.AsFloat(0) ?? 0;
        float rotateY = block.Attributes["rotate"]?["y"]?.AsFloat(0) ?? 0;
        float rotateZ = block.Attributes["rotate"]?["z"]?.AsFloat(0) ?? 0;

        JsonObject[] rotations = Array.Empty<JsonObject>();

        if (block.Attributes.KeyExists("rotations"))
        {
            rotations = block.Attributes["rotations"].AsArray();
        } 

        _inventoryTranformName = transform;
        Vec3f[] selectionBoxesTranslations = block.SelectionBoxes.Select(GetCentre).ToArray();

        _transforms = new float[selectionBoxesTranslations.Length][];
        for (int index = 0; index < selectionBoxesTranslations.Length; index++)
        {
            float x = selectionBoxesTranslations[index].X;
            float y = selectionBoxesTranslations[index].Y;
            float z = selectionBoxesTranslations[index].Z;

            float rotateXByIndex = 0;
            float rotateYByIndex = 0;
            float rotateZByIndex = 0;

            if (rotations.Length > index)
            {
                rotateXByIndex = rotations[index]["x"].AsFloat(0);
                rotateYByIndex = rotations[index]["y"].AsFloat(0);
                rotateZByIndex = rotations[index]["z"].AsFloat(0);
            }
            
            _transforms[index] = new Matrixf()
                .Translate(x, y, z)
                .RotateX(rotateXByIndex * GameMath.DEG2RAD)
                .RotateY(rotateYByIndex * GameMath.DEG2RAD)
                .RotateZ(rotateZByIndex * GameMath.DEG2RAD)
                .RotateX(rotateX * GameMath.DEG2RAD)
                .RotateY(rotateY * GameMath.DEG2RAD)
                .RotateZ(rotateZ * GameMath.DEG2RAD).Values;
        }
    }

    protected override string getMeshCacheKey(ItemStack stack)
    {
        IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
        if (containedMeshSource != null)
        {
            return containedMeshSource.GetMeshCacheKey(stack);
        }

        return $"{stack.Collectible.Code}.{_inventoryTranformName}";
    }

    protected static Vec3f GetCentre(Cuboidf box)
    {
        return new(
            box.X1 + (box.X2 - box.X1) / 2,
            box.Y1 + (box.Y2 - box.Y1) / 2,
            box.Z1 + (box.Z2 - box.Z1) / 2
            );
    }

    public virtual bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
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
        (Api as ICoreClientAPI)?.TriggerIngameError(this, "doesnotfit", Lang.Get("This item does not fit."));
        return true;
    }

    protected virtual bool TryPut(ItemSlot slot, BlockSelection blockSel, IPlayer player)
    {
        int index = blockSel.SelectionBoxIndex;

        if (_inventory[index].Empty)
        {
            int num = slot.TryPutInto(Api.World, _inventory[index]);
            if (num > 0)
            {
                updateMesh(index);
                MarkDirty(redrawOnClient: true);
            }
            return num > 0;
        }
        return false;
    }

    protected virtual bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
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
        return _transforms;
    }
}

public class GenericDisplayBlock : BlockDisplayCase
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        return (world.BlockAccessor.GetBlockEntity(blockSel.Position) as GenericDisplayBlockEntity)?.OnInteract(byPlayer, blockSel) ?? base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}
