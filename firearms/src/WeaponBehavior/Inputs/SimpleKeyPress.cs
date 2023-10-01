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

namespace MaltiezFirearms.WeaponBehavior.Inputs
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
