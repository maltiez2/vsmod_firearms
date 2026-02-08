using CombatOverhaul.Animations;
using CombatOverhaul.Armor;
using CombatOverhaul.Inputs;
using CombatOverhaul.RangedSystems;
using CombatOverhaul.Utils;
using csogg;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Firearms;

public interface IRangedWeaponIsLoaded
{
    bool GetIsLoaded(EntityPlayer player, ItemSlot slot);
}

public class PistolStats
{
    public string[] ReplacementItemWildcards { get; set; } = [];

    public string[] ReplacementBackpackCategoryWildcards { get; set; } = [];
}

public class PistolClient : MuzzleloaderClient
{
    public PistolClient(ICoreClientAPI api, Item item) : base(api, item)
    {
    }
}

public class PistolServer : MuzzleloaderServer, IRangedWeaponIsLoaded
{
    public PistolServer(ICoreServerAPI api, Item item) : base(api, item)
    {
        PistolStats = item.Attributes.AsObject<PistolStats>();
    }


    public override bool Shoot(IServerPlayer player, ItemSlot slot, ShotPacket packet, Entity shooter)
    {
        bool result = base.Shoot(player, slot, packet, shooter);

        if (!result)
        {
            return false;
        }

        ItemSlot? replaceWith = FindReplacementSlot(player);
        if (replaceWith == null)
        {
            return true;
        }

        ReplaceWeapon(player, replaceWith, slot);

        return true;
    }

    public bool GetIsLoaded(EntityPlayer player, ItemSlot slot)
    {
        Inventory.Read(slot, InventoryId);

        bool loaded = Inventory.Items.Count > 0;

        Inventory.Clear();

        return loaded;
    }


    protected readonly PistolStats PistolStats;


    protected virtual void ReplaceWeapon(IServerPlayer player, ItemSlot from, ItemSlot to)
    {
        DummySlot temporarySlot = new();
        int transferred = to.TryPutInto(player.Entity.Api.World, temporarySlot);
        if (transferred == 0)
        {
            return;
        }
        to.MarkDirty();

        transferred = from.TryPutInto(player.Entity.Api.World, to);
        if (transferred == 0)
        {
            temporarySlot.TryPutInto(player.Entity.Api.World, from);
            return;
        }
        from.MarkDirty();

        bool returnedCurrentWeapon = player.Entity.TryGiveItemStack(temporarySlot.Itemstack);
        if (returnedCurrentWeapon)
        {
            return;
        }

        player.Entity.Api.World.SpawnItemEntity(temporarySlot.Itemstack, player.Entity.Pos.XYZ, player.Entity.Pos.Motion);
    }

    protected virtual ItemSlot? FindReplacementSlot(IServerPlayer player)
    {
        InventoryPlayerBackPacks? inventory = GetBackpackInventory(player);
        if (inventory == null)
        {
            return null;
        }

        foreach (ItemSlot slot in inventory)
        {
            if (slot is not ItemSlotBagContentWithWildcardMatch bagSlot)
            {
                continue;
            }

            if (MatchBagSlot(bagSlot) && bagSlot.Itemstack.Collectible is IRangedWeaponIsLoaded loadedWeapon && loadedWeapon.GetIsLoaded(player.Entity, bagSlot))
            {
                return slot;
            }
        }

        foreach (ItemSlot slot in inventory)
        {
            if (slot is not ItemSlotBagContentWithWildcardMatch bagSlot)
            {
                continue;
            }

            if (MatchBagSlot(bagSlot))
            {
                return slot;
            }
        }

        return null;
    }

    protected virtual bool MatchBagSlot(ItemSlotBagContentWithWildcardMatch bagSlot)
    {
        if (bagSlot.Empty)
        {
            return false;
        }

        if (PistolStats.ReplacementBackpackCategoryWildcards.Length > 0 && !PistolStats.ReplacementBackpackCategoryWildcards.Any(wildcard => WildcardUtil.Match(wildcard, bagSlot.BackpackCategoryCode)))
        {
            return false;
        }
        
        if (PistolStats.ReplacementItemWildcards.Length > 0)
        {
            string itemCode = bagSlot.Itemstack.Collectible?.Code?.ToString() ?? "";
            if (!PistolStats.ReplacementItemWildcards.Any(wildcard => WildcardUtil.Match(wildcard, itemCode)))
            {
                return false;
            }
        }

        return true;
    }

    protected virtual InventoryPlayerBackPacks? GetBackpackInventory(IServerPlayer player)
    {
        return player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName) as InventoryPlayerBackPacks;
    }
}

public class PistolItem : Item, IHasWeaponLogic, IHasRangedWeaponLogic, IHasIdleAnimations, IRangedWeaponIsLoaded
{
    public PistolClient? ClientLogic { get; private set; }
    public PistolServer? ServerLogic { get; private set; }

    public AnimationRequestByCode IdleAnimation { get; private set; }
    public AnimationRequestByCode ReadyAnimation { get; private set; }

    public MuzzleloaderStats? Stats { get; private set; }

    IClientWeaponLogic? IHasWeaponLogic.ClientLogic => ClientLogic;
    IServerRangedWeaponLogic? IHasRangedWeaponLogic.ServerWeaponLogic => ServerLogic;

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
    {
        base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);

        GeneralUtils.MarkItemStack(outputSlot);
        outputSlot.MarkDirty();
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (Stats != null && Stats.ProficiencyStat != "")
        {
            string description = Lang.Get("combatoverhaul:iteminfo-proficiency", Lang.Get($"combatoverhaul:proficiency-{Stats.ProficiencyStat}"));
            dsc.AppendLine(description);
        }

        if (Stats != null)
        {
            dsc.AppendLine(Lang.Get("combatoverhaul:iteminfo-range-weapon-damage", Stats.BulletDamageMultiplier, Stats.BulletDamageStrength));
            dsc.AppendLine("");
        }

        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }

    public bool GetIsLoaded(EntityPlayer player, ItemSlot slot) => ServerLogic?.GetIsLoaded(player, slot) ?? false;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api is ICoreClientAPI clientAPI)
        {
            ClientLogic = new(clientAPI, this);

            Stats = Attributes.AsObject<MuzzleloaderStats>();
            IdleAnimation = new(Stats.IdleAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            ReadyAnimation = new(Stats.ReadyAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
        }

        if (api is ICoreServerAPI serverAPI)
        {
            ServerLogic = new(serverAPI, this);
        }
    }
}