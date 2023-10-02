using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    internal class Factory<ProductClass, IdGeneratorClass> : IFactory<ProductClass>
        where ProductClass : IFactoryObject
        where IdGeneratorClass : IUniqueIdGeneratorForFactory, new()  
    {
        private readonly Dictionary<string, Type> mProducts = new();
        private readonly IUniqueIdGeneratorForFactory mIdGenerator = new IdGeneratorClass();

        public Type GetType(string name)
        {
            return mProducts[name];
        }
        public void RegisterType<ObjectClass>(string name) where ObjectClass : ProductClass, new()
        {
            mProducts.Add(name, typeof(ObjectClass));
        }
        public ProductClass Instantiate(string name, JsonObject definition, CollectibleObject collectible)
        {
            ProductClass producedInstance = (ProductClass)Activator.CreateInstance(mProducts[name]);
            producedInstance.Init(definition, collectible);
            producedInstance.SetId(mIdGenerator.GenerateInstanceId());
            return producedInstance;
        }
    }
}
