using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.FiniteStateMachine.API.IKeyInput;
using MaltiezFirearms.FiniteStateMachine.API;
using static MaltiezFirearms.FiniteStateMachine.API.IMouseInput;
using Vintagestory.API.Client;
using System;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class BasicMouse : BaseInput, IMouseInput
    {
        public const string keyAttrName = "key";
        public const string keyPressTypeAttrName = "type";
        public const string altAttrName = "alt";
        public const string ctrlAttrName = "ctrl";
        public const string shiftAttrName = "shift";
        public const string repeatAttrName = "repeat";

        private string mKey;
        private bool mRepeatable;
        private EnumMouseButton mKeyEnum;
        private MouseEventType mType;
        private KeyPressModifiers mModifiers;
        private ICoreClientAPI mClientApi;

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            base.Init(code, definition, collectible, api);

            mKey = definition[keyAttrName].AsString();
            mRepeatable = definition[repeatAttrName].AsBool(false);
            mKeyEnum = (EnumMouseButton)Enum.Parse(typeof(EnumMouseButton), mKey);
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

            mClientApi = api as ICoreClientAPI;
        }

        public MouseEventType GetEventType()
        {
            return mType;
        }
        public bool CheckIfShouldBeHandled(MouseEvent mouseEvent, MouseEventType eventType)
        {
            if (mClientApi == null) throw new InvalidOperationException("BasicMouse.CheckIfShouldBeHandled() called on server side");

            if (mType != eventType) return false;
            if (mouseEvent.Button != mKeyEnum) return false;
            if (!ClientCheckModifiers()) return false;

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

        public bool IsRepeatable()
        {
            return mRepeatable;
        }

        protected bool ClientCheckModifiers()
        {        
            bool altPressed = mClientApi.Input.KeyboardKeyState[(int)GlKeys.AltLeft] || mClientApi.Input.KeyboardKeyState[(int)GlKeys.AltRight];
            bool ctrlPressed = mClientApi.Input.KeyboardKeyState[(int)GlKeys.ControlLeft] || mClientApi.Input.KeyboardKeyState[(int)GlKeys.ControlRight];
            bool shiftPressed = mClientApi.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] || mClientApi.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];

            return mModifiers.Alt == altPressed && mModifiers.Ctrl == ctrlPressed && mModifiers.Shift == shiftPressed;
        }
    }
}
