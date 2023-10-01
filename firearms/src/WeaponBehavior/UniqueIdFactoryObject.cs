using MaltiezFirearms.WeaponBehavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior
{
    public abstract class UniqueIdFactoryObject : IFactoryObject
    {
        private int mId;

        public abstract void Init(JsonObject definition, CollectibleObject collectible);
        
        public void SetId(int id) => mId = id;
        public int GetId() => mId;
        public override bool Equals(object obj) => (obj as UniqueIdFactoryObject)?.GetId() == mId;
        public override int GetHashCode() => mId;
    }
}
