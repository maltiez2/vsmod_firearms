using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace MaltiezFirearms
{
    public abstract class WeaponSystem : CollectibleBehavior
    {
        protected WeaponSystem(CollectibleObject collObj) : base(collObj)
        {
            
        }
    }
}