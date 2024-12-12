using CombatOverhaul;
using CombatOverhaul.Animations;
using CombatOverhaul.Implementations;
using CombatOverhaul.Inputs;
using CombatOverhaul.RangedSystems;
using CombatOverhaul.RangedSystems.Aiming;
using System.Numerics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Firearms;

public enum MuzzleloaderState
{
    Unloaded,
    Loading,
    Loaded,
    Priming,
    Primed,
    Cocking,
    Cocked,
    Aim,
    Shoot
}

public enum MuzzleloaderLoadingStage
{
    Unloaded,
    Loading,
    Priming
}

public class MuzzleloaderStats : WeaponStats
{
    public string ReadyAnimationOffhand { get; set; } = "";
    public string IdleAnimationOffhand { get; set; } = "";

    public string[] LoadAnimation { get; set; } = Array.Empty<string>();
    public string PrimeAnimation { get; set; } = "";
    public string CockingAnimation { get; set; } = "";
    public string CockingAnimationOffhand { get; set; } = "";
    public string AimAnimation { get; set; } = "";
    public string[] ShootAnimation { get; set; } = Array.Empty<string>();
    public string AimAnimationOffhand { get; set; } = "";
    public string[] ShootAnimationOffhand { get; set; } = Array.Empty<string>();
    public string LoadedAnimation { get; set; } = "";
    public string PrimedAnimation { get; set; } = "";
    public string CockedAnimation { get; set; } = "";

    public string LoadTpAnimation { get; set; } = "";
    public string PrimeTpAnimation { get; set; } = "";
    public string AimTpAnimation { get; set; } = "";

    public float LoadSpeedPenalty { get; set; } = -1.0f;
    public float PrimeSpeedPenalty { get; set; } = -0.3f;

    public AimingStatsJson Aiming { get; set; } = new();
    public float[] DispersionMOA { get; set; } = new float[] { 0, 0 };
    public float BulletDamageMultiplier { get; set; } = 1;
    public float BulletDamageStrength { get; set; } = 1;
    public float BulletVelocity { get; set; } = 1;
    public string BulletWildcard { get; set; } = "*bullet-*";
    public float Zeroing { get; set; } = 0;

    public int MagazineSize { get; set; } = 1;
    public int BulletsFiredPerShot { get; set; } = 1;
    public int BulletLoadedPerReload { get; set; } = 1;
    public int WaddingUsedPerReload { get; set; } = 1;
    public int LoadPowderConsumption { get; set; } = 1;
    public int PrimePowderConsumption { get; set; } = 1;
    public string FlaskWildcard { get; set; } = "*powderflask-*";
    public string WaddingWildcard { get; set; } = "*linenpatch*";
    public string LoadingRequirementWildcard { get; set; } = "";
    public string PrimingRequirementWildcard { get; set; } = "";
    public string CockingRequirementWildcard { get; set; } = "";
    public string LoadingRequirementMessage { get; set; } = "maltiezfirearms:requirement-missing-loading-equipment";
    public string PrimingRequirementMessage { get; set; } = "maltiezfirearms:requirement-missing-priming-equipment";
    public string CockingRequirementMessage { get; set; } = "maltiezfirearms:requirement-missing-cocking-equipment";
    public bool CancelReloadOnInAir { get; set; } = true;
}

public class MuzzleloaderClient : RangeWeaponClient
{
    public MuzzleloaderClient(ICoreClientAPI api, Item item) : base(api, item)
    {
        Attachable = item.GetCollectibleBehavior<AnimatableAttachable>(withInheritance: true) ?? throw new Exception("Firearm should have AnimatableAttachable behavior.");
        AimingSystem = api.ModLoader.GetModSystem<CombatOverhaulSystem>().AimingSystem ?? throw new Exception();
        BulletTransform = new(item.Attributes["BulletTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        FlaskTransform = new(item.Attributes["FlaskTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        PrimingEquipmentTransform = new(item.Attributes["PrimingEquipmentTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        LoadingEquipmentTransform = new(item.Attributes["LoadingEquipmentTransform"].AsObject<ModelTransformNoDefaults>() ?? new ModelTransformNoDefaults(), ModelTransform.BlockDefaultTp());
        Stats = item.Attributes.AsObject<MuzzleloaderStats>();
        AimingStats = Stats.Aiming.ToStats();

        AnimationsManager.RegisterTransformByCode(BulletTransform, $"Bullet - {item.Code}");
        AnimationsManager.RegisterTransformByCode(FlaskTransform, $"Flask - {item.Code}");
        AnimationsManager.RegisterTransformByCode(LoadingEquipmentTransform, $"Loading - {item.Code}");
        AnimationsManager.RegisterTransformByCode(PrimingEquipmentTransform, $"Priming - {item.Code}");
    }

    public override void OnSelected(ItemSlot slot, EntityPlayer player, bool mainHand, ref int state)
    {
        Attachable.ClearAttachments(player.EntityId);
        AimingSystem.AimingState = WeaponAimingState.None;

        MuzzleloaderLoadingStage stage = GetLoadingStage<MuzzleloaderLoadingStage>(slot);
        state = stage switch
        {
            MuzzleloaderLoadingStage.Unloaded => (int)MuzzleloaderState.Unloaded,
            MuzzleloaderLoadingStage.Loading => (int)MuzzleloaderState.Loaded,
            MuzzleloaderLoadingStage.Priming => (int)MuzzleloaderState.Primed,
            _ => (int)MuzzleloaderState.Unloaded
        };

        switch (stage)
        {
            case MuzzleloaderLoadingStage.Loading:
                AnimationBehavior?.Play(mainHand, Stats.LoadedAnimation, category: ItemAnimationCategory(mainHand), weight: 0.001f);
                break;
            case MuzzleloaderLoadingStage.Priming:
                AnimationBehavior?.Play(mainHand, Stats.PrimedAnimation, category: ItemAnimationCategory(mainHand), weight: 0.001f);
                break;
        }

        // DEBUG
        //DebugAttach(player);
    }
    public override void OnDeselected(EntityPlayer player, bool mainHand, ref int state)
    {
        Attachable.ClearAttachments(player.EntityId);
        AimingAnimationController?.Stop(mainHand);
        AimingSystem.AimingState = WeaponAimingState.None;
        AimingSystem.StopAiming();
        AnimationBehavior?.StopAllVanillaAnimations(mainHand);
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
    protected readonly MuzzleloaderStats Stats;
    protected readonly AimingStats AimingStats;
    protected readonly ItemInventoryBuffer Inventory = new();
    protected readonly ModelTransform BulletTransform;
    protected readonly ModelTransform FlaskTransform;
    protected readonly ModelTransform LoadingEquipmentTransform;
    protected readonly ModelTransform PrimingEquipmentTransform;
    protected const string InventoryId = "magazine";
    protected const string LoadingStageAttribute = "CombatOverhaul:loading-stage";
    protected ItemSlot? BulletSlot;

    protected const string PlayerStatsMainHandCategory = "CombatOverhaul:held-item-mainhand";
    protected const string PlayerStatsOffHandCategory = "CombatOverhaul:held-item-offhand";

    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Active)]
    protected virtual bool Load(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, MuzzleloaderState.Unloaded)) return false;
        if (eventData.AltPressed || !mainHand || !CheckForOtherHandEmpty(mainHand, player)) return false;
        if (Stats.LoadingRequirementWildcard != "" && !CheckRequirement(Stats.LoadingRequirementWildcard, player))
        {
            Api.TriggerIngameError(this, "missingRequirement", Lang.Get(Stats.LoadingRequirementMessage));
            return false;
        }
        if (!CheckWadding(player) || !CheckFlask(player, Stats.LoadPowderConsumption)) return false;

        ItemSlot? ammoSlot = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

            if (
                slot.Itemstack?.Item != null &&
                slot.Itemstack.Item.HasBehavior<ProjectileBehavior>() &&
                WildcardUtil.Match(Stats.BulletWildcard, slot.Itemstack.Item.Code.ToString()) &&
                slot.Itemstack.StackSize >= Stats.BulletLoadedPerReload)
            {
                ammoSlot = slot;
                return false;
            }

            return true;
        });

        if (ammoSlot == null)
        {
            Api.TriggerIngameError(this, "missingAmmo", Lang.Get("maltiezfirearms:requirement-missing-ammo"));
            return false;
        }

        bool lastAmmoToLoad = SpaceLeftInMagazine(slot) == 1;

        SetState(MuzzleloaderState.Loading, mainHand);
        AnimationBehavior?.Stop(ItemAnimationCategory(mainHand));
        AnimationBehavior?.Play(
            mainHand,
            GetLoadingAnimation(slot),
            category: AnimationCategory(mainHand),
            animationSpeed: GetAnimationSpeed(player, Stats.ProficiencyStat),
            callback: () => LoadCallback(slot, player, mainHand, lastAmmoToLoad),
            callbackHandler: callback => LoadAnimationCallback(callback, ammoSlot, player));
        AnimationBehavior?.PlayVanillaAnimation(Stats.LoadTpAnimation, mainHand);

        Attachable.SetAttachment(player.EntityId, "bullet", ammoSlot.Itemstack, BulletTransform);

        PlayerBehavior?.SetStat("walkspeed", mainHand ? PlayerStatsMainHandCategory : PlayerStatsOffHandCategory, Stats.LoadSpeedPenalty);

        return true;
    }
    protected virtual void LoadAnimationCallback(string callback, ItemSlot bulletSlot, EntityPlayer player)
    {
        switch (callback)
        {
            case "attach":
                {
                    Attachable.SetAttachment(player.EntityId, "bullet", bulletSlot.Itemstack, BulletTransform);

                    /*ItemSlot? waddingSlot = null;
                    player.WalkInventory(slot =>
                    {
                        if (slot?.Itemstack?.Item == null) return true;

                        if (WildcardUtil.Match(Stats.WaddingWildcard, slot.Itemstack.Item.Code.ToString()))
                        {
                            waddingSlot = slot;
                            return false;
                        }

                        return true;
                    });
                    if (waddingSlot != null) Attachable.SetAttachment(player.EntityId, "wadding", waddingSlot.Itemstack, WaddingTransform);*/

                    ItemSlot? flaskSlot = null;
                    player.WalkInventory(slot =>
                    {
                        if (slot?.Itemstack?.Item == null) return true;

                        if (WildcardUtil.Match(Stats.FlaskWildcard, slot.Itemstack.Item.Code.ToString()))
                        {
                            flaskSlot = slot;
                            return false;
                        }

                        return true;
                    });
                    if (flaskSlot != null) Attachable.SetAttachment(player.EntityId, "flask", flaskSlot.Itemstack, FlaskTransform);

                    ItemSlot? loadingSlot = null;
                    player.WalkInventory(slot =>
                    {
                        if (slot?.Itemstack?.Item == null) return true;

                        if (WildcardUtil.Match(Stats.LoadingRequirementWildcard, slot.Itemstack.Item.Code.ToString()))
                        {
                            loadingSlot = slot;
                            return false;
                        }

                        return true;
                    });
                    if (loadingSlot != null) Attachable.SetAttachment(player.EntityId, "loading", loadingSlot.Itemstack, LoadingEquipmentTransform);
                }
                break;
            case "detach":
                {
                    Attachable.ClearAttachments(player.EntityId);
                }
                break;
        }
    }
    protected virtual bool LoadCallback(ItemSlot slot, EntityPlayer player, bool mainHand, bool lastAmmoToLoad)
    {
        if (CheckState(mainHand, MuzzleloaderState.Loading))
        {
            ItemSlot? ammoSlot = null;

            player.WalkInventory(slot =>
            {
                if (slot?.Itemstack?.Item == null) return true;

                if (
                    slot.Itemstack?.Item != null &&
                    slot.Itemstack.Item.HasBehavior<ProjectileBehavior>() &&
                    WildcardUtil.Match(Stats.BulletWildcard, slot.Itemstack.Item.Code.ToString()) &&
                    slot.Itemstack.StackSize >= Stats.BulletLoadedPerReload)
                {
                    ammoSlot = slot;
                    return false;
                }

                return true;
            });

            if (ammoSlot == null) return true;

            PutIntoMagazine(slot, ammoSlot);
            RangedWeaponSystem.Reload(slot, ammoSlot, Stats.BulletLoadedPerReload, mainHand, success => LoadServerCallback(success, lastAmmoToLoad, mainHand), data: SerializeLoadingStage(MuzzleloaderLoadingStage.Loading));
            SetState(lastAmmoToLoad ? MuzzleloaderState.Loaded : MuzzleloaderState.Unloaded, mainHand);
        }
        Attachable.ClearAttachments(player.EntityId);
        AnimationBehavior?.Play(mainHand, Stats.LoadedAnimation, category: ItemAnimationCategory(mainHand), weight: 0.001f);
        AnimationBehavior?.PlayReadyAnimation(mainHand);
        PlayerBehavior?.SetStat("walkspeed", mainHand ? PlayerStatsMainHandCategory : PlayerStatsOffHandCategory);
        return true;
    }
    protected virtual void LoadServerCallback(bool success, bool lastAmmoToLoad, bool mainHand)
    {

    }

    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Active)]
    protected virtual bool Prime(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, MuzzleloaderState.Loaded)) return false;
        if (eventData.AltPressed || !mainHand || !CheckForOtherHandEmpty(mainHand, player)) return false;
        if (Stats.PrimingRequirementWildcard != "" && !CheckRequirement(Stats.PrimingRequirementWildcard, player))
        {
            Api.TriggerIngameError(this, "missingRequirement", Lang.Get(Stats.PrimingRequirementMessage));
            return false;
        }
        if (!CheckFlask(player, Stats.PrimePowderConsumption)) return false;

        SetState(MuzzleloaderState.Priming, mainHand);
        AnimationBehavior?.Stop(ItemAnimationCategory(mainHand));
        AnimationBehavior?.Play(mainHand, Stats.PrimeAnimation, category: AnimationCategory(mainHand), animationSpeed: GetAnimationSpeed(player, Stats.ProficiencyStat), callback: () => PrimeCallback(mainHand, slot), callbackHandler: callback => PrimeAnimationCallback(callback, player));
        AnimationBehavior?.PlayVanillaAnimation(Stats.PrimeTpAnimation, mainHand);

        PlayerBehavior?.SetStat("walkspeed", mainHand ? PlayerStatsMainHandCategory : PlayerStatsOffHandCategory, Stats.PrimeSpeedPenalty);

        return true;
    }
    protected virtual void PrimeAnimationCallback(string callback, EntityPlayer player)
    {
        switch (callback)
        {
            case "attach":
                {
                    ItemSlot? flaskSlot = null;
                    player.WalkInventory(slot =>
                    {
                        if (slot?.Itemstack?.Item == null) return true;

                        if (WildcardUtil.Match(Stats.FlaskWildcard, slot.Itemstack.Item.Code.ToString()))
                        {
                            flaskSlot = slot;
                            return false;
                        }

                        return true;
                    });
                    if (flaskSlot != null) Attachable.SetAttachment(player.EntityId, "flask", flaskSlot.Itemstack, FlaskTransform);

                    ItemSlot? primingSlot = null;
                    player.WalkInventory(slot =>
                    {
                        if (slot?.Itemstack?.Item == null) return true;

                        if (WildcardUtil.Match(Stats.PrimingRequirementWildcard, slot.Itemstack.Item.Code.ToString()))
                        {
                            primingSlot = slot;
                            return false;
                        }

                        return true;
                    });
                    if (primingSlot != null) Attachable.SetAttachment(player.EntityId, "priming", primingSlot.Itemstack, PrimingEquipmentTransform);
                }
                break;
            case "detach":
                {
                    Attachable.ClearAttachments(player.EntityId);
                }
                break;
        }
    }
    protected virtual bool PrimeCallback(bool mainHand, ItemSlot slot)
    {
        if (CheckState(mainHand, MuzzleloaderState.Priming))
        {
            RangedWeaponSystem.Load(slot, mainHand, success => PrimeServerCallback(success, mainHand), data: SerializeLoadingStage(MuzzleloaderLoadingStage.Priming));
        }
        AnimationBehavior?.PlayReadyAnimation(mainHand);
        AnimationBehavior?.Play(mainHand, Stats.PrimedAnimation, category: ItemAnimationCategory(mainHand), weight: 0.001f);
        PlayerBehavior?.SetStat("walkspeed", mainHand ? PlayerStatsMainHandCategory : PlayerStatsOffHandCategory);
        return true;
    }
    protected virtual void PrimeServerCallback(bool success, bool mainHand)
    {
        if (CheckState(true, MuzzleloaderState.Priming) && success)
        {
            SetState(MuzzleloaderState.Primed, mainHand);
        }
        else
        {
            SetState(MuzzleloaderState.Loaded, mainHand);
        }
    }

    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Active)]
    protected virtual bool Cocking(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, MuzzleloaderState.Primed)) return false;
        if (eventData.AltPressed) return false;
        if (Stats.CockingRequirementWildcard != "" && !CheckRequirement(Stats.CockingRequirementWildcard, player))
        {
            Api.TriggerIngameError(this, "missingRequirement", Lang.Get(Stats.CockingRequirementMessage));
            return false;
        }
        if (Stats.CockingAnimation == "")
        {
            SetState(MuzzleloaderState.Cocked, mainHand);
            return false;
        }

        AnimationBehavior?.Play(mainHand, mainHand ? Stats.CockingAnimation : Stats.CockingAnimationOffhand, category: AnimationCategory(mainHand), animationSpeed: GetAnimationSpeed(player, Stats.ProficiencyStat), callback: () => CockingCallback(mainHand));
        SetState(MuzzleloaderState.Cocking, mainHand);

        return true;
    }
    protected virtual bool CockingCallback(bool mainHand)
    {
        if (CheckState(mainHand, MuzzleloaderState.Cocking)) SetState(MuzzleloaderState.Cocked, mainHand);
        return true;
    }

    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Active)]
    protected virtual bool Aim(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, MuzzleloaderState.Cocked)) return false;
        if (!mainHand && !CanUseOffhand(player)) return false;
        if (eventData.AltPressed) return false;

        SetState(MuzzleloaderState.Aim, mainHand);
        AnimationBehavior?.Play(mainHand, mainHand ? Stats.AimAnimation : Stats.AimAnimationOffhand, category: AnimationCategory(mainHand));
        AnimationBehavior?.PlayVanillaAnimation(Stats.AimTpAnimation, mainHand);
        AimingSystem.AimingState = WeaponAimingState.FullCharge;
        AimingSystem.StartAiming(AimingStats);
        AimingAnimationController?.Play(mainHand);

        return true;
    }
    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Released)]
    protected virtual bool Ease(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        PlayerBehavior?.SetStat("walkspeed", mainHand ? PlayerStatsMainHandCategory : PlayerStatsOffHandCategory);

        switch ((MuzzleloaderState)state)
        {
            case MuzzleloaderState.Loading:
                {
                    SetState(MuzzleloaderState.Unloaded, mainHand);
                    Attachable.ClearAttachments(player.EntityId);
                }
                break;
            case MuzzleloaderState.Priming:
                {
                    SetState(MuzzleloaderState.Loaded, mainHand);
                    AnimationBehavior?.Play(mainHand, Stats.LoadedAnimation, category: ItemAnimationCategory(mainHand), weight: 0.001f);
                    Attachable.ClearAttachments(player.EntityId);
                }
                break;
            case MuzzleloaderState.Cocking:
                {
                    SetState(MuzzleloaderState.Primed, mainHand);
                }
                break;
            case MuzzleloaderState.Aim:
            case MuzzleloaderState.Shoot:
                {
                    Inventory.Read(slot, InventoryId);
                    if (Inventory.Items.Count == 0)
                    {
                        SetState(MuzzleloaderState.Unloaded, mainHand);
                    }
                    else
                    {
                        SetState(MuzzleloaderState.Cocked, mainHand);
                        AnimationBehavior?.Play(mainHand, Stats.CockedAnimation != "" ? Stats.CockedAnimation : Stats.PrimedAnimation, category: ItemAnimationCategory(mainHand), weight: 0.001f);
                    }
                    Inventory.Clear();
                }
                break;
            default:
                break;
        }

        AnimationBehavior?.PlayReadyAnimation(mainHand);
        AimingSystem.StopAiming();
        AimingAnimationController?.Stop(mainHand);
        AnimationBehavior?.StopAllVanillaAnimations(mainHand);
        return false;
    }

    [ActionEventHandler(EnumEntityAction.LeftMouseDown, ActionState.Pressed)]
    protected virtual bool Shoot(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (!CheckState(state, MuzzleloaderState.Aim)) return false;
        if (eventData.AltPressed) return false;

        Inventory.Read(slot, InventoryId);
        if (Inventory.Items.Count == 0)
        {
            Inventory.Clear();
            return false;
        }
        Inventory.Clear();

        SetState(MuzzleloaderState.Shoot, mainHand);
        AnimationBehavior?.Stop(ItemAnimationCategory(mainHand));
        AnimationBehavior?.Play(
            mainHand,
            GetShootingAnimation(mainHand, slot),
            category: AnimationCategory(mainHand),
            callback: () => ShootCallback(slot, player, mainHand),
            callbackHandler: callback => ShootAnimationCallback(callback, slot, player, mainHand));

        return true;
    }
    protected virtual bool ShootCallback(ItemSlot slot, EntityPlayer player, bool mainHand)
    {
        AnimationBehavior?.Play(mainHand, mainHand ? Stats.AimAnimation : Stats.AimAnimationOffhand, category: AnimationCategory(mainHand));
        SetState(MuzzleloaderState.Aim, mainHand);

        return true;
    }
    protected virtual void ShootAnimationCallback(string callback, ItemSlot slot, EntityPlayer player, bool mainHand)
    {
        switch (callback)
        {
            case "shoot":
                Vintagestory.API.MathTools.Vec3d position = player.LocalEyePos + player.Pos.XYZ;
                Vector3 targetDirection = AimingSystem.TargetVec;

                targetDirection = ClientAimingSystem.Zeroing(targetDirection, Stats.Zeroing);

                RangedWeaponSystem.Shoot(slot, Stats.BulletsFiredPerShot, new((float)position.X, (float)position.Y, (float)position.Z), new(targetDirection.X, targetDirection.Y, targetDirection.Z), mainHand, ShootServerCallback);

                break;
        }
    }
    protected virtual void ShootServerCallback(bool success)
    {

    }

    [ActionEventHandler(EnumEntityAction.RightMouseDown, ActionState.Active)]
    protected virtual bool Cancel(ItemSlot slot, EntityPlayer player, ref int state, ActionEventData eventData, bool mainHand, AttackDirection direction)
    {
        if (player.OnGround || !Stats.CancelReloadOnInAir) return false;

        switch ((MuzzleloaderState)state)
        {
            case MuzzleloaderState.Loading:
                {
                    PlayerBehavior?.SetStat("walkspeed", mainHand ? PlayerStatsMainHandCategory : PlayerStatsOffHandCategory);
                    SetState(MuzzleloaderState.Unloaded, mainHand);
                    Attachable.ClearAttachments(player.EntityId);

                    AnimationBehavior?.PlayReadyAnimation(mainHand);
                    AimingSystem.StopAiming();
                    AimingAnimationController?.Stop(mainHand);
                }
                break;
            default:
                break;
        }

        return false;
    }

    protected void PutIntoMagazine(ItemSlot slot, ItemSlot ammoSlot)
    {
        Inventory.Read(slot, InventoryId);
        ItemStack ammo = ammoSlot.TakeOut(Stats.BulletLoadedPerReload);
        Inventory.Items.Add(ammo);
        Inventory.Write(slot);
        Inventory.Clear();
    }
    protected int LeftInMagazine(ItemSlot slot)
    {
        Inventory.Read(slot, InventoryId);
        int size = Inventory.Items.Count;
        Inventory.Clear();

        return size;
    }
    protected int SpaceLeftInMagazine(ItemSlot slot)
    {
        Inventory.Read(slot, InventoryId);
        int size = Stats.MagazineSize - Inventory.Items.Count;
        Inventory.Clear();

        return size;
    }
    protected string GetLoadingAnimation(ItemSlot slot)
    {
        if (Stats.MagazineSize == 1) return Stats.LoadAnimation[0];

        Inventory.Read(slot, InventoryId);

        int ammoCount = Inventory.Items.Count;
        int animationsCount = Stats.LoadAnimation.Length;
        int index = ammoCount % animationsCount;

        Inventory.Clear();

        return Stats.LoadAnimation[index];
    }
    protected string GetShootingAnimation(bool mainhand, ItemSlot slot)
    {
        string[] animations = mainhand ? Stats.ShootAnimation : Stats.ShootAnimationOffhand;

        if (Stats.MagazineSize == 1) return animations[0];

        Inventory.Read(slot, InventoryId);

        int ammoCount = Inventory.Items.Count;
        int animationsCount = animations.Length;
        int index = ammoCount % animationsCount;

        Inventory.Clear();

        return animations[index];
    }
    protected bool CheckWadding(EntityPlayer player)
    {
        ItemSlot? waddingSlot = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

            if (
                WildcardUtil.Match(Stats.WaddingWildcard, slot.Itemstack.Item.Code.ToString()) &&
                slot.Itemstack.StackSize >= Stats.WaddingUsedPerReload)
            {
                waddingSlot = slot;
                return false;
            }

            return true;
        });
        
        bool found = waddingSlot != null;
        if (!found)
        {
            Api.TriggerIngameError(this, "missingWadding", Lang.Get("maltiezfirearms:requirement-missing-wadding"));
        }
        return found;
    }
    protected bool CheckFlask(EntityPlayer player, int powderNeeded)
    {
        ItemSlot? flaskSlot = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

            if (
                WildcardUtil.Match(Stats.FlaskWildcard, slot.Itemstack.Item.Code.ToString()) &&
                slot.Itemstack.Item.GetRemainingDurability(slot.Itemstack) >= powderNeeded)
            {
                flaskSlot = slot;
                return false;
            }

            return true;
        });

        bool found = flaskSlot != null;
        if (!found)
        {
            Api.TriggerIngameError(this, "missingPowderflask", Lang.Get("maltiezfirearms:requirement-missing-powderflask"));
        }
        return found;
    }

    protected static byte[] SerializeLoadingStage<TStage>(TStage stage)
        where TStage : struct, Enum
    {

        int stageInt = (int)Enum.ToObject(typeof(TStage), stage);
        return BitConverter.GetBytes(stageInt);
    }
    protected static TStage GetLoadingStage<TStage>(ItemSlot slot)
        where TStage : struct, Enum
    {
        int stage = slot.Itemstack?.Attributes.GetAsInt(LoadingStageAttribute, 0) ?? 0;
        return (TStage)Enum.ToObject(typeof(TStage), stage);
    }
    protected static bool CheckRequirement(string requirementWildcard, EntityPlayer player)
    {
        ItemSlot? flaskSlot = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

            if (
                WildcardUtil.Match(requirementWildcard, slot.Itemstack.Item.Code.ToString()))
            {
                flaskSlot = slot;
                return false;
            }

            return true;
        });
        return flaskSlot != null;
    }

    protected void DebugAttach(EntityPlayer player)
    {
        ItemSlot? ammoSlot1 = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

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
            if (slot?.Itemstack?.Item == null) return true;

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

        ItemSlot? ammoSlot4 = null;
        player.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

            if (WildcardUtil.Match(Stats.PrimingRequirementWildcard, slot.Itemstack.Item.Code.ToString()))
            {
                ammoSlot4 = slot;
                return false;
            }

            return true;
        });
        if (ammoSlot4 != null) Attachable.SetAttachment(player.EntityId, "priming", ammoSlot4.Itemstack, PrimingEquipmentTransform);
    }

    protected string AnimationCategory(bool mainHand) => mainHand ? "main" : "mainOffhand";
    protected string ItemAnimationCategory(bool mainHand) => mainHand ? "item" : "itemOffhand";

    protected bool CanUseOffhand(EntityPlayer player)
    {
        if (player.RightHandItemSlot?.Itemstack?.Item is not MuzzleloaderItem weapon) return true;

        MuzzleloaderState state = GetState<MuzzleloaderState>(mainHand: true);
        return state switch
        {
            MuzzleloaderState.Unloaded => true,
            MuzzleloaderState.Loading => true,
            MuzzleloaderState.Loaded => true,
            MuzzleloaderState.Priming => true,
            MuzzleloaderState.Primed => true,
            MuzzleloaderState.Cocking => true,
            MuzzleloaderState.Cocked => false,
            MuzzleloaderState.Aim => false,
            MuzzleloaderState.Shoot => false,
            _ => false
        };
    }
}

public class MuzzleloaderServer : RangeWeaponServer
{
    public MuzzleloaderServer(ICoreServerAPI api, Item item) : base(api, item)
    {
        Stats = item.Attributes.AsObject<MuzzleloaderStats>();
    }

    public override bool Reload(IServerPlayer player, ItemSlot slot, ItemSlot? ammoSlot, ReloadPacket packet)
    {
        MuzzleloaderLoadingStage currentStage = GetLoadingStage<MuzzleloaderLoadingStage>(packet);
        //MuzzleloaderLoadingStage finishedStage = GetLoadingStage<MuzzleloaderLoadingStage>(slot);
        //if (currentStage < finishedStage) return false;

        int powderNeeded = currentStage switch
        {
            MuzzleloaderLoadingStage.Loading => Stats.LoadPowderConsumption,
            MuzzleloaderLoadingStage.Priming => Stats.PrimePowderConsumption,
            _ => 1
        };

        ItemSlot? flask = GetFlask(player, powderNeeded);
        ItemSlot? wadding = GetWadding(player);

        if (flask?.Itemstack?.Item == null || (wadding?.Itemstack?.Item == null && currentStage == MuzzleloaderLoadingStage.Loading)) return false;

        if (ammoSlot != null)
        {
            Inventory.Read(slot, InventoryId);
            if (Inventory.Items.Count >= Stats.MagazineSize) return false;

            if (
                ammoSlot.Itemstack?.Item != null &&
                ammoSlot.Itemstack.Item.HasBehavior<ProjectileBehavior>() &&
                WildcardUtil.Match(Stats.BulletWildcard, ammoSlot.Itemstack.Item.Code.ToString()) &&
                ammoSlot.Itemstack.StackSize >= Stats.BulletLoadedPerReload)
            {
                for (int count = 0; count < Stats.BulletLoadedPerReload; count++)
                {
                    ItemStack ammo = ammoSlot.TakeOut(1);
                    Inventory.Items.Add(ammo);
                }

                ammoSlot.MarkDirty();
                Inventory.Write(slot);
                Inventory.Clear();
            }
            else
            {
                return false;
            }
        }

        flask.Itemstack.Attributes.SetInt("durability", flask.Itemstack.Collectible.GetRemainingDurability(flask.Itemstack) - powderNeeded);
        flask.MarkDirty();

        if (currentStage == MuzzleloaderLoadingStage.Loading)
        {
            wadding.TakeOut(Stats.WaddingUsedPerReload);
            wadding.MarkDirty();
        }

        SetLoadingStage(slot, currentStage);

        return true;
    }

    public override bool Shoot(IServerPlayer player, ItemSlot slot, ShotPacket packet, Entity shooter)
    {
        MuzzleloaderLoadingStage finishedStage = GetLoadingStage<MuzzleloaderLoadingStage>(slot);
        if (finishedStage != LastStage) return false;

        Inventory.Read(slot, InventoryId);

        if (Inventory.Items.Count == 0) return false;

        int count = 0;
        int additionalDurabilityCost = 0;
        while (Inventory.Items.Count > 0 && count < packet.Amount)
        {
            ItemStack ammo = Inventory.Items[0];
            ammo.ResolveBlockOrItem(Api.World);
            Inventory.Items.RemoveAt(0);

            ProjectileStats? stats = ammo.Item?.GetCollectibleBehavior<ProjectileBehavior>(true)?.Stats;

            if (stats == null)
            {
                continue;
            }

            additionalDurabilityCost = Math.Max(additionalDurabilityCost, stats.AdditionalDurabilityCost);

            ProjectileSpawnStats spawnStats = new()
            {
                ProducerEntityId = player.Entity.EntityId,
                DamageMultiplier = Stats.BulletDamageMultiplier,
                DamageStrength = Stats.BulletDamageStrength,
                Position = new Vector3(packet.Position[0], packet.Position[1], packet.Position[2]),
                Velocity = GetDirectionWithDispersion(packet.Velocity, Stats.DispersionMOA) * Stats.BulletVelocity
            };

            ProjectileSystem.Spawn(packet.ProjectileId[count], stats, spawnStats, ammo, shooter);

            count++;
        }

        slot.Itemstack.Item.DamageItem(player.Entity.World, player.Entity, slot, 1 + additionalDurabilityCost);
        slot.MarkDirty();

        Inventory.Write(slot);
        int ammoLeft = Inventory.Items.Count;
        Inventory.Clear();

        if (ammoLeft == 0) SetLoadingStage(slot, MuzzleloaderLoadingStage.Unloaded);

        return true;
    }

    protected readonly MuzzleloaderStats Stats;
    protected readonly ItemInventoryBuffer Inventory = new();
    protected const string InventoryId = "magazine";
    protected const string LoadingStageAttribute = "CombatOverhaul:loading-stage";
    protected readonly MuzzleloaderLoadingStage LastStage = MuzzleloaderLoadingStage.Priming;

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

    protected ItemSlot? GetFlask(IServerPlayer player, int powderNeeded)
    {
        ItemSlot? flaskSlot = null;
        player.Entity.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

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
        player.Entity.WalkInventory(slot =>
        {
            if (slot?.Itemstack?.Item == null) return true;

            if (
                WildcardUtil.Match(Stats.WaddingWildcard, slot.Itemstack.Item.Code.ToString()) &&
                slot.Itemstack.StackSize >= Stats.WaddingUsedPerReload)
            {
                waddingSlot = slot;
                return false;
            }

            return true;
        });
        return waddingSlot;
    }
}

public class MuzzleloaderItem : Item, IHasWeaponLogic, IHasRangedWeaponLogic, IHasDynamicIdleAnimations
{
    public MuzzleloaderClient? ClientLogic { get; private set; }
    public MuzzleloaderServer? ServerLogic { get; private set; }

    public AnimationRequestByCode IdleAnimation { get; private set; }
    public AnimationRequestByCode ReadyAnimation { get; private set; }
    public AnimationRequestByCode IdleAnimationOffhand { get; private set; }
    public AnimationRequestByCode ReadyAnimationOffhand { get; private set; }

    public MuzzleloaderStats? Stats { get; private set; }

    IClientWeaponLogic? IHasWeaponLogic.ClientLogic => ClientLogic;
    IServerRangedWeaponLogic? IHasRangedWeaponLogic.ServerWeaponLogic => ServerLogic;

    public AnimationRequestByCode? GetIdleAnimation(bool mainHand) => mainHand ? IdleAnimation : IdleAnimationOffhand;
    public AnimationRequestByCode? GetReadyAnimation(bool mainHand) => mainHand ? ReadyAnimation : ReadyAnimationOffhand;

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
}