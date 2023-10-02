using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.FiniteStateMachine.API.IKeyInput;
using MaltiezFirearms.FiniteStateMachine.API;
using static MaltiezFirearms.FiniteStateMachine.API.IMouseInput;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class BasicMouse : UniqueIdFactoryObject, IMouseInput
    {
        public const string keyAttrName = "key";
        public const string keyPressTypeAttrName = "type";
        public const string altAttrName = "alt";
        public const string ctrlAttrName = "ctrl";
        public const string shiftAttrName = "shift";

        private string mCode;
        private string mKey;
        private MouseEventType mType;
        private KeyPressModifiers mModifiers;

        public override void Init(JsonObject definition, CollectibleObject collectible)
        {
            mCode = definition["code"].AsString();
            mKey = definition[keyAttrName].AsString();
            switch (definition[keyPressTypeAttrName].AsString())
            {
                case ("released"):
                    mType = MouseEventType.MOUSE_UP;
                    break;
                case ("pressed"):
                    mType = MouseEventType.MOUSE_DOWN;
                    break;
                default:
                    mType = MouseEventType.MOUSE_DOWN;
                    break;

            }
            mModifiers = new KeyPressModifiers
            (
                definition[altAttrName].AsBool(false),
                definition[ctrlAttrName].AsBool(false),
                definition[shiftAttrName].AsBool(false)
            );
        }

        public MouseEventType GetEventType()
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
