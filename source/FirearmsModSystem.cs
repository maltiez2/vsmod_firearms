﻿using ConfigLib;
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

        api.RegisterCollectibleBehaviorClass("Firearms:Wettable", typeof(Wettable));
        api.RegisterCollectibleBehaviorClass("Firearms:Igniteable", typeof(Igniteable));

        if (api.ModLoader.IsModEnabled("configlib"))
        {
            SubscribeToConfigChange(api);
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
        };
    }
}
