using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    public class WeaponSystemPrototype : UniqueIdFactoryObject, IWeaponSystem
    {
        public override void Init(JsonObject definition, CollectibleObject colelctible)
        {

        }
        public bool Verify(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters)
        {
            return true;
        }
        public bool Process(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters)
        {
            return true;
        }
    }
}
