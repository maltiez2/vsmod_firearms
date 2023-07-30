using Vintagestory.API.Common;

namespace MaltiezFirearms
{
    public class FirearmsMod : ModSystem
    {
		public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("MaltiezFirearm", typeof(ItemFirearm));
            api.RegisterItemClass("MaltiezPowderFlask", typeof(ItemPowderFlask)); 
            api.RegisterItemClass("MaltiezMatch", typeof(ItemMatch));
            api.RegisterItemClass("MaltiezMusket", typeof(ItemMusket));
        }
	}
}
