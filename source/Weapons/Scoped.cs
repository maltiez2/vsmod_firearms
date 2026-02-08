using CombatOverhaul.Animations;
using CombatOverhaul.Inputs;
using CombatOverhaul.RangedSystems;
using CombatOverhaul.RangedSystems.Aiming;
using CombatOverhaul.Utils;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Firearms;

public class ScopedClient : MuzzleloaderClient
{
    public ScopedClient(ICoreClientAPI api, Item item) : base(api, item)
    {
        IronsidesAimingStats = item.Attributes["IronsidesAiming"].AsObject<AimingStatsJson>().ToStats();
        ScopeAimingStats = item.Attributes["ScopeAiming"].AsObject<AimingStatsJson>().ToStats();
        ScopedAimAnimation = item.Attributes["ScopedAimAnimation"].AsString("");
        ScopedShootAnimation = item.Attributes["ScopedShootAnimation"].AsObject<string[]>();
    }

    protected readonly AimingStats IronsidesAimingStats;
    protected readonly AimingStats ScopeAimingStats;
    protected readonly string ScopedAimAnimation;
    protected readonly string[] ScopedShootAnimation;
    protected bool Scoped;


    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Active)]
    protected override bool Aim(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, MuzzleloaderState.Cocked)) return false;
        if (!mainHand && !CanUseOffhand(player)) return false;
        if (eventData.AltPressed) return false;
        if (eventData.Modifiers.Contains(EnumEntityAction.CtrlKey)) return false;

        SetState(MuzzleloaderState.Aim, mainHand);
        AnimationBehavior?.Play(mainHand, Scoped ? ScopedAimAnimation : Stats.AimAnimation , category: AnimationCategory(mainHand));
        TpAnimationBehavior?.Play(mainHand, Scoped ? ScopedAimAnimation : Stats.AimAnimation, category: AnimationCategory(mainHand));
        AnimationBehavior?.StopAllVanillaAnimations(mainHand);
        if (TpAnimationBehavior == null) AnimationBehavior?.PlayVanillaAnimation(Stats.AimTpAnimation, mainHand);
        AimingSystem.StartAiming(Scoped ? ScopeAimingStats : IronsidesAimingStats);
        AimingSystem.AimingState = WeaponAimingState.FullCharge;
        AimingAnimationController.Stats = Scoped ? ScopeAimingStats : IronsidesAimingStats;
        AimingAnimationController.Play(mainHand);

        return true;
    }

    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Pressed)]
    protected virtual bool ChangeSites(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, MuzzleloaderState.Cocked)) return false;
        if (!mainHand && !CanUseOffhand(player)) return false;
        if (eventData.AltPressed) return false;
        if (!eventData.Modifiers.Contains(EnumEntityAction.CtrlKey)) return false;

        Scoped = !Scoped;

        return true;
    }

    protected override bool ShootCallback(ItemSlot slot, EntityPlayer player, bool mainHand)
    {
        AnimationBehavior?.Play(mainHand, Scoped ? ScopedAimAnimation : Stats.AimAnimation, category: AnimationCategory(mainHand));
        TpAnimationBehavior?.Play(mainHand, Scoped ? ScopedAimAnimation : Stats.AimAnimation, category: AnimationCategory(mainHand));
        SetState(MuzzleloaderState.Aim, mainHand);

        return true;
    }

    /*protected override string GetShootingAnimation(bool mainhand, ItemSlot slot)
    {
        string[] animations = Scoped ? ScopedShootAnimation : Stats.ShootAnimation;

        if (Stats.MagazineSize == 1) return animations[0];

        Inventory.Read(slot, InventoryId);

        int ammoCount = Inventory.Items.Count;
        int animationsCount = animations.Length;
        int index = ammoCount % animationsCount;

        Inventory.Clear();

        return animations[index];
    }*/
}

public class ScopedItem : Item, IHasWeaponLogic, IHasRangedWeaponLogic, IHasDynamicIdleAnimations
{
    public ScopedClient? ClientLogic { get; private set; }
    public MuzzleloaderServer? ServerLogic { get; private set; }

    public AnimationRequestByCode IdleAnimation { get; private set; }
    public AnimationRequestByCode ReadyAnimation { get; private set; }
    public AnimationRequestByCode IdleAnimationOffhand { get; private set; }
    public AnimationRequestByCode ReadyAnimationOffhand { get; private set; }

    public MuzzleloaderStats? Stats { get; private set; }

    IClientWeaponLogic? IHasWeaponLogic.ClientLogic => ClientLogic;
    IServerRangedWeaponLogic? IHasRangedWeaponLogic.ServerWeaponLogic => ServerLogic;

    public AnimationRequestByCode? GetIdleAnimation(EntityPlayer player, ItemSlot slot, bool mainHand) => mainHand ? IdleAnimation : IdleAnimationOffhand;
    public AnimationRequestByCode? GetReadyAnimation(EntityPlayer player, ItemSlot slot, bool mainHand) => mainHand ? ReadyAnimation : ReadyAnimationOffhand;

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (Stats != null)
        {
            dsc.AppendLine(Lang.Get("combatoverhaul:iteminfo-range-weapon-damage", Stats.BulletDamageMultiplier, Stats.BulletDamageStrength));
            dsc.AppendLine("");
        }
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api is ICoreClientAPI clientAPI)
        {
            ClientLogic = new(clientAPI, this);

            Stats = Attributes.AsObject<MuzzleloaderStats>();
            IdleAnimation = new(Stats.IdleAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            ReadyAnimation = new(Stats.ReadyAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            IdleAnimationOffhand = new(Stats.IdleAnimationOffhand, 1, 1, "mainOffhand", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            ReadyAnimationOffhand = new(Stats.ReadyAnimationOffhand, 1, 1, "mainOffhand", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
        }

        if (api is ICoreServerAPI serverAPI)
        {
            ServerLogic = new(serverAPI, this);
        }
    }

    public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
    {
        base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);

        GeneralUtils.MarkItemStack(outputSlot);
        outputSlot.MarkDirty();
    }
}