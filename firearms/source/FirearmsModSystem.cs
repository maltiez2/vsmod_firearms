using Vintagestory.API.Common;

namespace Firearms;

internal class FirearmsModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass("Firearms:GenericDisplayBlock", typeof(GenericDisplayBlock));
        api.RegisterBlockEntityClass("Firearms:GenericDisplayBlockEntity", typeof(GenericDisplayBlockEntity));
    }
}
