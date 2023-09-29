using firearms.src;
using HarmonyLib;
using ProtoBuf;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Server;

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
