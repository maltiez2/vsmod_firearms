using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class BasicSlotBefore : BaseInput, ISlotChangedBefore
    {
        public const string typeAttrName = "type";
        public readonly Dictionary<string, EnumHandling> types = new Dictionary<string, EnumHandling> // ImmutableDictionary are just pain in the ass to deal with, so deal with regular one being not immutable!
        {
            { "prevent",    EnumHandling.PreventSubsequent },
            { "handle",     EnumHandling.Handled }
        };
        
        private EnumHandling mInputType;

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            base.Init(code, definition, collectible, api);

            mInputType = types[definition[typeAttrName].AsString()];
        }

        public EnumHandling GetHandlingType()
        {
            return mInputType;
        }
    }
}

