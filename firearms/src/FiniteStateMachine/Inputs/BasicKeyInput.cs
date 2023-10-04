using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.FiniteStateMachine.API.IKeyInput;
using MaltiezFirearms.FiniteStateMachine.API;
using Vintagestory.API.Client;
using System;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class BasicKey : BaseInput, IKeyInput
    {
        public const string keyAttrName = "key";
        public const string keyPressTypeAttrName = "type";
        public const string altAttrName = "alt";
        public const string ctrlAttrName = "ctrl";
        public const string shiftAttrName = "shift";

        private string mKey;
        private KeyEventType mType;
        private int mKeyEnum;
        private KeyPressModifiers mModifiers;
        private ICoreClientAPI mClientApi;

        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            base.Init(name, definition, collectible, api);

            mKey = definition[keyAttrName].AsString();
            mKeyEnum = (int)Enum.Parse(typeof(GlKeys), mKey);
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

            mClientApi = api as ICoreClientAPI;
        }

        public KeyEventType GetEventType()
        {
            return mType;
        }
        public bool CheckIfShouldBeHandled(KeyEvent keyEvent, KeyEventType eventType)
        {
            if (mClientApi == null) throw new InvalidOperationException("BasicKey.CheckIfShouldBeHandled() called on server side");

            if (mType != eventType) return false;
            if (keyEvent.KeyCode != mKeyEnum) return false;
            if (keyEvent.AltPressed != mModifiers.Alt || keyEvent.CtrlPressed != mModifiers.Ctrl || keyEvent.ShiftPressed != mModifiers.Shift) return false;

            return true;
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
