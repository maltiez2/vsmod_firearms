using CombatOverhaul.Animations;
using CombatOverhaul.Inputs;
using CombatOverhaul.RangedSystems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Firearms;


public class MatchlockClient : MuzzleloaderClient
{
    public MatchlockClient(ICoreClientAPI api, Item item) : base(api, item)
    {
    }

    protected override bool ShootCallback(ItemSlot slot, EntityPlayer player, bool mainHand)
    {
        //AnimationBehavior?.Play(mainHand, mainHand ? Stats.AimAnimation : Stats.AimAnimationOffhand);
        SetState(MuzzleloaderState.Aim);

        return true;
    }
}

public class MatchlockItem : Item, IHasWeaponLogic, IHasRangedWeaponLogic, IHasIdleAnimations
{
    public MatchlockClient? ClientLogic { get; private set; }
    public MuzzleloaderServer? ServerLogic { get; private set; }

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