using Vintagestory.API.Common;

namespace Firearms;

internal class FirearmsModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass("Firearms:ViceBlock", typeof(ViceBlock));
        api.RegisterBlockEntityClass("Firearms:ViceEntity", typeof(ViceEntity));
    }
}
