using CombatOverhaul;
using CombatOverhaul.Animations;
using CombatOverhaul.Implementations;
using CombatOverhaul.Inputs;
using CombatOverhaul.RangedSystems;
using CombatOverhaul.RangedSystems.Aiming;
using OpenTK.Mathematics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Firearms;

public enum RevolverState
{
    Idle,
    StartLoading,
    Loading,
    FinishLoading,
    Cocking,
    Aim,
    Shoot
}

public enum RevolverReadyStage
{
    Unloaded,
    Ready,
    Fired
}

public class RevolverLoadStageStats
{
    public string LoadedAnimation { get; set; } = "";
    public string UnloadedAnimation { get; set; } = "";
    public string LoadingAnimation { get; set; } = "";
    public string CockingAnimation { get; set; } = "";
    public string CockedAnimation { get; set; } = "";
    public string ShootAnimation { get; set; } = "";
    public string FiredAnimation { get; set; } = "";
    public string AimAnimation { get; set; } = "";
    public string StartLoadingAnimation { get; set; } = "";
    public string FinishLoadingAnimation { get; set; } = "";

    public int LoadToStage { get; set; } = 0;
    public int UnloadToStage { get; set; } = 0;

    public int WaddingUsed { get; set; } = 1;
    public int PowderConsumption { get; set; } = 1;
    public int BulletsFired { get; set; } = 1;
    public int BulletLoaded { get; set; } = 1;

    public float SpeedPenalty { get; set; } = -2.0f;

    public RevolverFiringStats? FiringStats { get; set; } = null;
    public AimingStatsJson? Aiming { get; set; } = null;
}

public class RevolverFiringStats
{
    public float[] DispersionMOA { get; set; } = [0, 0];
    public float BulletDamageMultiplier { get; set; } = 1;
    public float BulletDamageStrength { get; set; } = 1;
    public float BulletVelocity { get; set; } = 1;
    public float Zeroing { get; set; } = 0;
}

public class RevolverStats : WeaponStats
{
    public RevolverLoadStageStats[] LoadStages { get; set; } = [];
    public RevolverFiringStats FiringStats { get; set; } = new();
    public AimingStatsJson Aiming { get; set; } = new();

    public string BulletWildcard { get; set; } = "*bullet-*";
    public string FlaskWildcard { get; set; } = "*powderflask-*";
    public string WaddingWildcard { get; set; } = "*linenpatch*";
    public string LoadingRequirementWildcard { get; set; } = "";
    public string LoadingRequirementMessage { get; set; } = "maltiezfirearms:requirement-missing-loading-equipment";

    public bool CancelReloadOnInAir { get; set; } = true;
    public float ReloadAnimationSpeed { get; set; } = 1;
}

public class RevolverClient : RangeWeaponClient
{
    public RevolverClient(ICoreClientAPI api, Item item) : base(api, item)
    {
        Attachable = item.GetCollectibleBehavior<AnimatableAttachable>(withInheritance: true) ?? throw new Exception("Firearm should have AnimatableAttachable behavior.");
        AimingSystem = api.ModLoader.GetModSystem<CombatOverhaulSystem>().AimingSystem ?? throw new Exception();
        BulletTransform = new(item.Attributes["BulletTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        FlaskTransform = new(item.Attributes["FlaskTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        PrimingEquipmentTransform = new(item.Attributes["PrimingEquipmentTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        LoadingEquipmentTransform = new(item.Attributes["LoadingEquipmentTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        Stats = item.Attributes.AsObject<RevolverStats>();
        AimingStats = Stats.Aiming.ToStats();

#if DEBUG
        DebugWindowManager.RegisterTransformByCode(BulletTransform, $"Bullet - {item.Code}");
        DebugWindowManager.RegisterTransformByCode(FlaskTransform, $"Flask - {item.Code}");
        DebugWindowManager.RegisterTransformByCode(LoadingEquipmentTransform, $"Loading - {item.Code}");
        DebugWindowManager.RegisterTransformByCode(PrimingEquipmentTransform, $"Priming - {item.Code}");
#endif

        FirearmsModSystem system = api.ModLoader.GetModSystem<FirearmsModSystem>();
        system.SettingsChanged += settings =>
        {
            AimingStats.CursorType = Enum.Parse<AimingCursorType>(settings.AimingCursorType);
        };

        //DebugWidgets.FloatDrag("test", "test", $"{item.Code}-followX", () => AimingStats.AnimationFollowX, (value) => AimingStats.AnimationFollowX = value);
        //DebugWidgets.FloatDrag("test", "test", $"{item.Code}-followY", () => AimingStats.AnimationFollowY, (value) => AimingStats.AnimationFollowY = value);
    }

    public override void OnSelected(ItemSlot slot, EntityPlayer player, bool mainHand, ref int state)
    {
        Attachable.ClearAttachments(player.EntityId);
        AimingSystem.AimingState = WeaponAimingState.None;
        // DEBUG

        int loadStage = GetLoadStage(slot);
        RevolverReadyStage readyStage = GetReadyStage(slot);
        RevolverLoadStageStats stageStats = Stats.LoadStages[loadStage];

        state = (int)RevolverState.Idle;

        switch (readyStage)
        {
            case RevolverReadyStage.Ready:
                //AnimationBehavior?.Play(mainHand, stageStats.LoadedAnimation, category: LoadingGatesAnimationCategory);
                //AnimationBehavior?.Play(mainHand, stageStats.CockedAnimation, category: LockAnimationCategory);
                break;
            case RevolverReadyStage.Unloaded:
                //AnimationBehavior?.Play(mainHand, stageStats.UnloadedAnimation, category: LoadingGatesAnimationCategory);
                break;
            case RevolverReadyStage.Fired:
                //AnimationBehavior?.Play(mainHand, stageStats.UnloadedAnimation, category: ItemAnimationCategory(mainHand));
                //AnimationBehavior?.Play(mainHand, stageStats.FiredAnimation, category: LockAnimationCategory);
                break;
        }

#if DEBUG
        DebugAttach(player);
#endif
    }
    public override void OnDeselected(EntityPlayer player, bool mainHand, ref int state)
    {
        Attachable.ClearAttachments(player.EntityId);
        AimingAnimationController?.Stop(mainHand);
        AimingSystem.AimingState = WeaponAimingState.None;
        AimingSystem.StopAiming();
        PlayerBehavior?.SetStat("walkspeed", mainHand ? PlayerStatsMainHandCategory : PlayerStatsOffHandCategory);
    }
    public override void OnRegistered(ActionsManagerPlayerBehavior behavior, ICoreClientAPI api)
    {
        base.OnRegistered(behavior, api);
        AimingAnimationController = new(AimingSystem, AnimationBehavior, AimingStats);
    }

    protected AimingAnimationController? AimingAnimationController;
    protected readonly AnimatableAttachable Attachable;
    protected readonly ClientAimingSystem AimingSystem;
    protected readonly RevolverStats Stats;
    protected readonly AimingStats AimingStats;
    protected readonly ItemInventoryBuffer Inventory = new();
    protected readonly ModelTransform BulletTransform;
    protected readonly ModelTransform FlaskTransform;
    protected readonly ModelTransform LoadingEquipmentTransform;
    protected readonly ModelTransform PrimingEquipmentTransform;
    protected const string InventoryId = "magazine";
    protected const string LoadingStageAttribute = "CombatOverhaul:loading-stage";
    protected const string WeaponReadyStageAttribute = "CombatOverhaul:ready-stage";
    protected ItemSlot? BulletSlot;

    protected const string PlayerStatsMainHandCategory = "CombatOverhaul:held-item-mainhand";
    protected const string PlayerStatsOffHandCategory = "CombatOverhaul:held-item-offhand";

    protected const string LoadingGatesAnimationCategory = "loading-gates";
    protected const string LockAnimationCategory = "lock";
    protected const string CylinderAnimationCategory = "cylinder";


    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Active)]
    protected virtual bool StartLoad(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, RevolverState.Idle) || !mainHand) return false;
        if (!eventData.Modifiers.Contains(EnumEntityAction.Sneak) ||
            eventData.Modifiers.Contains(EnumEntityAction.Forward) ||
            eventData.Modifiers.Contains(EnumEntityAction.Backward) ||
            eventData.Modifiers.Contains(EnumEntityAction.Left) ||
            eventData.Modifiers.Contains(EnumEntityAction.Right)) return false;
        if (InteractionsTester.PlayerTriesToInteract(player, mainHand, eventData)) return false;
        if (eventData.AltPressed || !CheckForOtherHandEmpty(mainHand, player)) return false;

        int loadStage = GetLoadStage(slot);
        RevolverReadyStage readyStage = GetReadyStage(slot);
        int maxLoadStage = Stats.LoadStages.Length - 1;

        if (readyStage == RevolverReadyStage.Fired) return false;
        if (readyStage == RevolverReadyStage.Ready && loadStage == maxLoadStage) return false;

        RevolverLoadStageStats currentStage = Stats.LoadStages[loadStage];

        AnimationBehavior?.Play(
            mainHand,
            currentStage.StartLoadingAnimation,
            animationSpeed: GetAnimationSpeed(player, Stats.ProficiencyStat),
            category: ItemAnimationCategory(mainHand),
            callback: () => StartLoadCallback(slot, player, mainHand, loadStage));

        SetState(RevolverState.StartLoading, mainHand);

        return true;
    }
    protected virtual bool StartLoadCallback(ItemSlot slot, EntityPlayer player, bool mainHand, int stage)
    {
        SetState(RevolverState.Loading, mainHand);

        RevolverLoadStageStats currentStage = Stats.LoadStages[stage];

        AnimationBehavior?.Play(
            mainHand,
            currentStage.StartLoadingAnimation,
            animationSpeed: GetAnimationSpeed(player, Stats.ProficiencyStat),
            category: ItemAnimationCategory(mainHand),
            callback: () => LoadStageCallback(slot, player, mainHand, stage),
            callbackHandler: code => LoadStageCallbackHandler(code, slot, player, mainHand, stage));

        return true;
    }
    protected virtual bool LoadStageCallback(ItemSlot slot, EntityPlayer player, bool mainHand, int stage)
    {
        RevolverLoadStageStats currentStage = Stats.LoadStages[stage];

        int nextStageIndex = currentStage.LoadToStage;

        if (nextStageIndex == stage)
        {
            AnimationBehavior?.Play(
                mainHand,
                currentStage.FinishLoadingAnimation,
                animationSpeed: GetAnimationSpeed(player, Stats.ProficiencyStat),
                category: ItemAnimationCategory(mainHand),
                callback: () => FinishLoadCallback(slot, player, mainHand, stage));

            SetState(RevolverState.FinishLoading, mainHand);

            return true;
        }

        RevolverLoadStageStats nextStage = Stats.LoadStages[nextStageIndex];

        AnimationBehavior?.Play(
            mainHand,
            nextStage.StartLoadingAnimation,
            animationSpeed: GetAnimationSpeed(player, Stats.ProficiencyStat),
            category: ItemAnimationCategory(mainHand),
            callback: () => LoadStageCallback(slot, player, mainHand, nextStageIndex),
            callbackHandler: code => LoadStageCallbackHandler(code, slot, player, mainHand, nextStageIndex));

        return true;
    }
    protected virtual void LoadStageCallbackHandler(string code, ItemSlot slot, EntityPlayer player, bool mainHand, int stage)
    {

    }
    protected virtual bool FinishLoadCallback(ItemSlot slot, EntityPlayer player, bool mainHand, int stage)
    {
        SetState(RevolverState.Idle, mainHand);

        return true;
    }




    protected int GetLoadStage(ItemSlot slot)
    {
        return slot.Itemstack?.Attributes.GetAsInt(LoadingStageAttribute, 0) ?? 0;
    }
    protected RevolverReadyStage GetReadyStage(ItemSlot slot)
    {
        return (RevolverReadyStage)(slot.Itemstack?.Attributes.GetAsInt(WeaponReadyStageAttribute, 0) ?? 0);
    }
    protected string AnimationCategory(bool mainHand) => mainHand ? "main" : "mainOffhand";
    protected string ItemAnimationCategory(bool mainHand) => mainHand ? "item" : "itemOffhand";
    protected void DebugAttach(EntityPlayer player)
    {
        ItemSlot? ammoSlot1 = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item?.Code == null) return true;

            if (WildcardUtil.Match(Stats.BulletWildcard, slot.Itemstack.Item.Code.ToString()))
            {
                ammoSlot1 = slot;
                return false;
            }

            return true;
        });
        if (ammoSlot1 != null) Attachable.SetAttachment(player.EntityId, "bullet", ammoSlot1.Itemstack, BulletTransform);

        ItemSlot? ammoSlot2 = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item?.Code == null) return true;

            if (WildcardUtil.Match(Stats.FlaskWildcard, slot.Itemstack.Item.Code.ToString()))
            {
                ammoSlot2 = slot;
                return false;
            }

            return true;
        });
        if (ammoSlot2 != null) Attachable.SetAttachment(player.EntityId, "flask", ammoSlot2.Itemstack, FlaskTransform);

        /*ItemSlot? ammoSlot3 = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

            if (WildcardUtil.Match(Stats.WaddingWildcard, slot.Itemstack.Item.Code.ToString()))
            {
                ammoSlot3 = slot;
                return false;
            }

            return true;
        });
        if (ammoSlot3 != null) Attachable.SetAttachment(player.EntityId, "wadding", ammoSlot3.Itemstack, WaddingTransform);*/

        /*ItemSlot? ammoSlot4 = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item?.Code == null) return true;

            if (WildcardUtil.Match(Stats.PrimingRequirementWildcard, slot.Itemstack.Item.Code.ToString()))
            {
                ammoSlot4 = slot;
                return false;
            }

            return true;
        });
        if (ammoSlot4 != null) Attachable.SetAttachment(player.EntityId, "priming", ammoSlot4.Itemstack, PrimingEquipmentTransform);*/
    }
}

public class RevolverServer : RangeWeaponServer
{
    public RevolverServer(ICoreServerAPI api, Item item) : base(api, item)
    {
        Stats = item.Attributes.AsObject<RevolverStats>();
    }

    public override bool Reload(IServerPlayer player, ItemSlot slot, ItemSlot? ammoSlot, ReloadPacket packet)
    {
        return true;
    }

    public override bool Shoot(IServerPlayer player, ItemSlot slot, ShotPacket packet, Entity shooter)
    {
        return true;
    }

    protected readonly RevolverStats Stats;
    protected readonly ItemInventoryBuffer Inventory = new();
    protected const string InventoryId = "magazine";
    protected const string LoadingStageAttribute = "CombatOverhaul:loading-stage";
    protected const string WeaponReadyStageAttribute = "CombatOverhaul:ready-stage";
    protected readonly int LastStage = 0;

    protected static TStage GetLoadingStage<TStage>(ReloadPacket packet)
        where TStage : struct, Enum
    {

        int stage = BitConverter.ToInt32(packet.Data, 0);
        return (TStage)Enum.ToObject(typeof(TStage), stage);
    }
    protected static TStage GetLoadingStage<TStage>(ItemSlot slot)
        where TStage : struct, Enum
    {
        int stage = slot.Itemstack?.Attributes.GetAsInt(LoadingStageAttribute, 0) ?? 0;
        return (TStage)Enum.ToObject(typeof(TStage), stage);
    }
    protected static void SetLoadingStage<TStage>(ItemSlot slot, TStage stage)
        where TStage : struct, Enum
    {
        slot.Itemstack?.Attributes.SetInt(LoadingStageAttribute, (int)Enum.ToObject(typeof(TStage), stage));
        slot.MarkDirty();
    }

    protected int GetLoadStage(ItemSlot slot)
    {
        return slot.Itemstack?.Attributes.GetAsInt(LoadingStageAttribute, 0) ?? 0;
    }
    protected RevolverReadyStage GetReadyStage(ItemSlot slot)
    {
        return (RevolverReadyStage)(slot.Itemstack?.Attributes.GetAsInt(WeaponReadyStageAttribute, 0) ?? 0);
    }

    protected ItemSlot? GetFlask(IServerPlayer player, int powderNeeded)
    {
        ItemSlot? flaskSlot = null;
        player.Entity.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item?.Code == null) return true;

            if (
                WildcardUtil.Match(Stats.FlaskWildcard, slot.Itemstack.Item.Code.ToString()) &&
                slot.Itemstack.Item.GetRemainingDurability(slot.Itemstack) >= powderNeeded)
            {
                flaskSlot = slot;
                return false;
            }

            return true;
        });
        return flaskSlot;
    }
    protected ItemSlot? GetWadding(IServerPlayer player)
    {
        ItemSlot? waddingSlot = null;
        /*player.Entity.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item?.Code == null) return true;

            if (
                WildcardUtil.Match(Stats.WaddingWildcard, slot.Itemstack.Item.Code.ToString()) &&
                slot.Itemstack.StackSize >= Stats.WaddingUsedPerReload)
            {
                waddingSlot = slot;
                return false;
            }

            return true;
        });*/
        return waddingSlot;
    }
}

public class RevolverItem : Item, IHasWeaponLogic, IHasRangedWeaponLogic, IHasDynamicIdleAnimations
{
    public RevolverClient? ClientLogic { get; private set; }
    public RevolverServer? ServerLogic { get; private set; }

    public AnimationRequestByCode IdleAnimation { get; private set; }
    public AnimationRequestByCode ReadyAnimation { get; private set; }
    public AnimationRequestByCode IdleAnimationOffhand { get; private set; }
    public AnimationRequestByCode ReadyAnimationOffhand { get; private set; }

    public RevolverStats? Stats { get; private set; }

    IClientWeaponLogic? IHasWeaponLogic.ClientLogic => ClientLogic;
    IServerRangedWeaponLogic? IHasRangedWeaponLogic.ServerWeaponLogic => ServerLogic;

    public AnimationRequestByCode? GetIdleAnimation(EntityPlayer player, ItemSlot slot, bool mainHand) => mainHand ? IdleAnimation : IdleAnimationOffhand;
    public AnimationRequestByCode? GetReadyAnimation(EntityPlayer player, ItemSlot slot, bool mainHand) => mainHand ? ReadyAnimation : ReadyAnimationOffhand;

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        if (Stats != null && Stats.ProficiencyStat != "")
        {
            string description = Lang.Get("combatoverhaul:iteminfo-proficiency", Lang.Get($"combatoverhaul:proficiency-{Stats.ProficiencyStat}"));
            dsc.AppendLine(description);
        }

        if (Stats != null)
        {
            //dsc.AppendLine(Lang.Get("combatoverhaul:iteminfo-range-weapon-damage", Stats.BulletDamageMultiplier, Stats.BulletDamageStrength));
            //dsc.AppendLine("");
        }
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
    }

    public override int GetRemainingDurability(ItemStack itemstack)
    {
        int durability = base.GetRemainingDurability(itemstack);
        int maxDurability = GetMaxDurability(itemstack);
        if (durability > maxDurability)
        {
            itemstack.Attributes.RemoveAttribute("durability");
            return maxDurability;
        }
        return durability;
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (api is ICoreClientAPI clientAPI)
        {
            ClientLogic = new(clientAPI, this);

            Stats = Attributes.AsObject<RevolverStats>();
            IdleAnimation = new(Stats.IdleAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            ReadyAnimation = new(Stats.ReadyAnimation, 1, 1, "main", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            //IdleAnimationOffhand = new(Stats.IdleAnimationOffhand, 1, 1, "mainOffhand", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
            //ReadyAnimationOffhand = new(Stats.ReadyAnimationOffhand, 1, 1, "mainOffhand", TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.2), false);
        }

        if (api is ICoreServerAPI serverAPI)
        {
            ServerLogic = new(serverAPI, this);
        }
    }
}