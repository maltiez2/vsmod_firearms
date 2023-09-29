using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    public class WeaponSystemPrototype : IWeaponSystem
    {
        public void Init(TreeAttribute definition, CollectibleObject colelctible)
        {

        }
        public bool Verify(ItemSlot weaponSlot, EntityAgent player, TreeAttribute parameters)
        {
            return true;
        }
        public bool Process(ItemSlot weaponSlot, EntityAgent player, TreeAttribute parameters)
        {
            return true;
        }
    }
}
