﻿using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    internal class Factory<TProductClass, TIdGeneratorClass> : IFactory<TProductClass>
        where TProductClass : IFactoryObject
        where TIdGeneratorClass : IUniqueIdGeneratorForFactory, new()  
    {
        private readonly Dictionary<string, Type> mProducts = new();
        private readonly IUniqueIdGeneratorForFactory mIdGenerator = new TIdGeneratorClass();

        public Type GetType(string name)
        {
            return mProducts[name];
        }
        public void RegisterType<TObjectClass>(string name) where TObjectClass : TProductClass, new()
        {
            mProducts.Add(name, typeof(TObjectClass));
        }
        public TProductClass Instantiate(string name, JsonObject definition, CollectibleObject collectible)
        {
            TProductClass producedInstance = (TProductClass)Activator.CreateInstance(mProducts[name]);
            producedInstance.Init(definition, collectible);
            producedInstance.SetId(mIdGenerator.GenerateInstanceId());
            return producedInstance;
        }
    }
}