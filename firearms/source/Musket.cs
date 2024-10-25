using CombatOverhaul.Animations;
using CombatOverhaul.Armor;
using CombatOverhaul.Inputs;
using CombatOverhaul.RangedSystems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Firearms;

public enum MusketLoadingStage
{
    Unloaded,
    Loading,
    Priming,
    AttachBayonet,
    DetachBayonet
}

public class MusketStats
{
    public string BayonetWildcard { get; set; } = "*bayonet-*";
}

public class MusketClient : MuzzleloaderClient
{
    public MusketClient(ICoreClientAPI api, Item item) : base(api, item)
    {
        StatsMusket = item.Attributes.AsObject<MusketStats>();
    }

    protected readonly ModelTransform BayonetTransform;
    protected readonly MusketStats StatsMusket;

    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Pressed)]
    protected virtual bool AttachBayonet(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (eventData.AltPressed || !mainHand || CheckForOtherHandEmpty(mainHand, player)) return false;
        if (!WildcardUtil.Match(StatsMusket.BayonetWildcard, player.LeftHandItemSlot.Itemstack?.Item?.Code?.ToString() ?? "")) return false;

        RangedWeaponSystem.Reload(slot, player.LeftHandItemSlot, 1, mainHand, ServerAttachBayonetCallback, data: SerializeLoadingStage(MusketLoadingStage.AttachBayonet));

        return true;
    }
    protected virtual void ServerAttachBayonetCallback(bool result)
    {
        
    }
}

public class MusketServer : MuzzleloaderServer
{
    public MusketServer(ICoreServerAPI api, Item item) : base(api, item)
    {
    }

    public override bool Reload(IServerPlayer player, ItemSlot slot, ItemSlot? ammoSlot, ReloadPacket packet)
    {
        MusketLoadingStage currentStage = GetLoadingStage<MusketLoadingStage>(packet);

        switch (currentStage)
        {
            case MusketLoadingStage.AttachBayonet:
                {
                    slot.Itemstack?.Attributes?.SetInt("renderVariant", 2);
                    slot.MarkDirty();
                }
                return true;
            case MusketLoadingStage.DetachBayonet:
                {
                    slot.Itemstack?.Attributes?.RemoveAttribute("renderVariant");
                    slot.MarkDirty();
                }
                return true;
            default:
                return base.Reload(player, slot, ammoSlot, packet);
        }
    }

    protected readonly ItemInventoryBuffer BayonetInventory = new();
    protected const string BayonetInventoryId = "bayonet";
}

public class MusketItem : Item, IHasWeaponLogic, IHasRangedWeaponLogic, IHasIdleAnimations
{
    public MusketClient? ClientLogic { get; private set; }
    public MusketServer? ServerLogic { get; private set; }

    public AnimationRequestByCode IdleAnimation { get; private set; }
    public AnimationRequestByCode ReadyAnimation { get; private set; }

    IClientWeaponLogic? IHasWeaponLogic.ClientLogic => ClientLogic;
    IServerRangedWeaponLogic? IHasRangedWeaponLogic.ServerWeaponLogic => ServerLogic;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api is ICoreClientAPI clientAPI)
        {
            ClientLogic = new(clientAPI, this);

            MuzzleloaderStats stats = Attributes.AsObject<MuzzleloaderStats>();
            IdleAnimation = new(stats.IdleAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            ReadyAnimation = new(stats.ReadyAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
        }

        if (api is ICoreServerAPI serverAPI)
        {
            ServerLogic = new(serverAPI, this);
        }
    }
}