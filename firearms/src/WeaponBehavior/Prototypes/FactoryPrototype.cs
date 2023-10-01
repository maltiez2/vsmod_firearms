using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    internal class FactoryPrototype<ProductClass, IdGeneratorClass> : IFactory<ProductClass>
        where ProductClass : IFactoryObject
        where IdGeneratorClass : IUniqueIdGeneratorForFactory, new()  
    {
        private readonly Dictionary<string, Type> mSystems = new();
        private readonly IUniqueIdGeneratorForFactory mIdGenerator = new IdGeneratorClass();

        public Type GetType(string name)
        {
            return mSystems[name];
        }
        public void RegisterType<ObjectClass>(string name) where ObjectClass : ProductClass, new()
        {
            mSystems.Add(name, typeof(ObjectClass));
        }
        public ProductClass Instantiate(string name, JsonObject definition, CollectibleObject collectible)
        {
            ProductClass producedInstance = (ProductClass)Activator.CreateInstance(mSystems[name]);
            producedInstance.Init(definition, collectible);
            producedInstance.SetId(mIdGenerator.GenerateInstanceId());
            return producedInstance;
        }
    }
}
