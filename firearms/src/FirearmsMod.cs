using Vintagestory.API.Common;

namespace MaltiezFirearms
{
    public class FirearmsMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("FirearmsNoMelee", typeof(NoMelee));
        }
    }
}
