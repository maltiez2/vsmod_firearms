using ConfigLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Firearms;

public class FirearmsSettings
{
    public string AimingCursorType { get; set; } = "None";
}

public class FirearmsModSystem : ModSystem
{
    public FirearmsSettings Settings { get; set; } = new();

    public event Action<FirearmsSettings>? SettingsChanged;

    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("Firearms:Muzzleloader", typeof(MuzzleloaderItem));
        api.RegisterItemClass("Firearms:Matchlock", typeof(MatchlockItem));
        api.RegisterItemClass("Firearms:Musket", typeof(MusketItem));
        api.RegisterItemClass("Firearms:PowderFlask", typeof(PowderFlask));
        api.RegisterItemClass("Firearms:Scoped", typeof(ScopedItem));
        api.RegisterItemClass("Firearms:Revolver", typeof(RevolverItem));

        api.RegisterCollectibleBehaviorClass("Firearms:Wettable", typeof(Wettable));
        api.RegisterCollectibleBehaviorClass("Firearms:Igniteable", typeof(Igniteable));

        if (api.ModLoader.IsModEnabled("configlib"))
        {
            SubscribeToConfigChange(api);
        }
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        if (api is ICoreClientAPI clientApi)
        {
            CheckStatusClientSide(clientApi);
        }
    }

    private void SubscribeToConfigChange(ICoreAPI api)
    {
        ConfigLibModSystem system = api.ModLoader.GetModSystem<ConfigLibModSystem>();

        system.SettingChanged += (domain, config, setting) =>
        {
            if (domain != "maltiezfirearms") return;

            setting.AssignSettingValue(Settings);

            SettingsChanged?.Invoke(Settings);
        };

        system.ConfigsLoaded += () =>
        {
            system.GetConfig("maltiezfirearms")?.AssignSettingsValues(Settings);
            SettingsChanged?.Invoke(Settings);
        };
    }

    private void CheckStatusClientSide(ICoreClientAPI api)
    {
        bool immersiveFirstPersonMode = api.Settings.Bool["immersiveFpMode"];
        if (immersiveFirstPersonMode)
        {
            CombatOverhaul.Utils.LoggerUtil.Error(api, this, $"Immersive first person mode is enabled. It is not supported. Turn this setting off.");
            AnnoyPlayer(api, "(Firearms) Immersive first person mode is enabled. It is not supported. Turn this setting off to prevent this message.", () => api.Settings.Bool["immersiveFpMode"]);
        }
    }
    private void AnnoyPlayer(ICoreClientAPI api, string message, System.Func<bool> continueDelegate)
    {
        api.World.RegisterCallback(_ =>
        {
            api.TriggerIngameError(this, "error", message);
            if (continueDelegate()) AnnoyPlayer(api, message, continueDelegate);
        }, 5000);
    }
}
