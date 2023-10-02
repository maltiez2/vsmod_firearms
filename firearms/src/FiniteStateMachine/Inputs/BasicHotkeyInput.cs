using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class BasicHotkey : UniqueIdFactoryObject, IHotkeyInput
    {
        public const string keyAttrName = "key";
        public const string keyPressTypeAttrName = "type";
        public const string altAttrName = "alt";
        public const string ctrlAttrName = "ctrl";
        public const string shiftAttrName = "shift";

        private string mCode;
        private string mKey;
        private KeyPressModifiers mModifiers;

        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mCode = definition["code"].AsString();
            mKey = definition[keyAttrName].AsString(); // @TODO @LOCAL Add localization
            mModifiers = new KeyPressModifiers
            (
                definition[altAttrName].AsBool(false),
                definition[ctrlAttrName].AsBool(false),
                definition[shiftAttrName].AsBool(false)
            );
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

