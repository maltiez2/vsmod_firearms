using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.WeaponBehavior.IInput;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    public class InputPrototype : IInput
    {
        public void Init(TreeAttribute definition, CollectibleObject colelctible)
        {

        }

        public string GetName()
        {
            return "testInput";
        }
        public InputType GetInputType()
        {
            return InputType.CLICK;
        }
        public Tuple<bool, bool, bool> GetIfAltCtrlShiftPressed()
        {
            return Tuple.Create(false, false, false);
        }
        public string GetKey()
        {
            return "R";
        }
    }
}
