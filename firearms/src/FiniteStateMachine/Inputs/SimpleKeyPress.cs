using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.FiniteStateMachine.API.IKeyInput;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class SimpleKeyPress : UniqueIdFactoryObject, IKeyInput
    {
        public const string KeyAttrName = "key";
        
        private string mCode;
        private string mKey;
        public override void Init(JsonObject definition, CollectibleObject collectible)
        {
            mCode = definition["code"].AsString();
            mKey = definition[KeyAttrName].AsString();
        }

        public KeyEventType GetEventType()
        {
            return KeyEventType.KeyDown;
        }

        public string GetName()
        {
            return mCode;
        }
        public KeyPressModifiers GetIfAltCtrlShiftPressed()
        {
            return new (false, false, false);
        }
        public string GetKey()
        {
            return mKey;
        }
    }
}
