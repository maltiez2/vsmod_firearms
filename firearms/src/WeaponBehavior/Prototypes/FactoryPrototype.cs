using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    internal class FactoryPrototype<ProducedClass> : IFactory<ProducedClass> where ProducedClass : IFactoryObject
    {
        private readonly Dictionary<string, Type> mSystems = new();

        public Type GetType(string name)
        {
            return mSystems[name];
        }
        public void RegisterType<ObjectClass>(string name) where ObjectClass : ProducedClass, new()
        {
            mSystems.Add(name, typeof(ObjectClass));
        }
        public ProducedClass Instantiate(string name, TreeAttribute definition, CollectibleObject colelctible)
        {
            ProducedClass weaponSystem = (ProducedClass)Activator.CreateInstance(mSystems[name]);
            weaponSystem.Init(definition, colelctible);
            return weaponSystem;
        }
    }
}
