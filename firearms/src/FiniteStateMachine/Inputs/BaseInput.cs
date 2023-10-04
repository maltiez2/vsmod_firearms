using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class BaseInput : UniqueIdFactoryObject, IInput
    {
        public const string codeAttrName = "key";
        public const string handledAttrName = "handle";

        private string mCode;
        private bool mHandled;

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mCode = code;
            mHandled = definition == null ? true : definition[handledAttrName].AsBool(true);
        }

        public string GetName()
        {
            return mCode;
        }
        public bool Handled()
        {
            return mHandled;
        }
    }
}

