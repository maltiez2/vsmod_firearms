﻿using Vintagestory.API.Common;

namespace Firearms;

internal class FirearmsModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("Firearms:Muzzleloader", typeof(MuzzleloaderItem));
        api.RegisterItemClass("Firearms:Matchlock", typeof(MatchlockItem));
        api.RegisterItemClass("Firearms:Musket", typeof(MusketItem));
        api.RegisterItemClass("Firearms:PowderFlask", typeof(PowderFlask));
        api.RegisterItemClass("Firearms:Scoped", typeof(ScopedItem));

        api.RegisterCollectibleBehaviorClass("Firearms:Wettable", typeof(Wettable));
        api.RegisterCollectibleBehaviorClass("Firearms:Igniteable", typeof(Igniteable));
    }
}
