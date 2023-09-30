using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.WeaponBehavior.IInput;
using static MaltiezFirearms.WeaponBehavior.IKeyInput;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    public class InputPrototype : UniqueIdFactoryObject, IKeyInput
    {
        public override void Init(JsonObject definition, CollectibleObject colelctible)
        {

        }

        public KeyEventType GetEventType()
        {
            return KeyEventType.KeyDown;
        }

        public string GetName()
        {
            return "testInput";
        }
        public KeyPressModifiers GetIfAltCtrlShiftPressed()
        {
            return new (false, false, false);
        }
        public string GetKey()
        {
            return "R";
        }
    }
}
