using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.FiniteStateMachine.API.IKeyInput;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class BasicKey : UniqueIdFactoryObject, IKeyInput
    {
        public const string keyAttrName = "key";
        public const string keyPressTypeAttrName = "type";
        public const string altAttrName = "alt";
        public const string ctrlAttrName = "ctrl";
        public const string shiftAttrName = "shift";

        private string mCode;
        private string mKey;
        private KeyEventType mType;
        private KeyPressModifiers mModifiers;

        public override void Init(JsonObject definition, CollectibleObject collectible)
        {
            mCode = definition["code"].AsString();
            mKey = definition[keyAttrName].AsString();
            switch (definition[keyPressTypeAttrName].AsString())
            {
                case ("released"):
                    mType = KeyEventType.KEY_UP;
                    break;
                case ("pressed"):
                    mType = KeyEventType.KEY_DOWN;
                    break;
                default:
                    mType = KeyEventType.KEY_DOWN;
                    break;

            }
            mModifiers = new KeyPressModifiers
            (
                definition[altAttrName].AsBool(false),
                definition[ctrlAttrName].AsBool(false),
                definition[shiftAttrName].AsBool(false)
            );
        }

        public KeyEventType GetEventType()
        {
            return mType;
        }

        public string GetName()
        {
            return mCode;
        }
        public KeyPressModifiers GetIfAltCtrlShiftPressed()
        {
            return mModifiers;
        }
        public string GetKey()
        {
            return mKey;
        }
    }
}
