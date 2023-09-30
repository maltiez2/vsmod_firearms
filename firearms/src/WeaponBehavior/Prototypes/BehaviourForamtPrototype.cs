using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior
{
    public class BehaviourFormatPrototype : IBehaviourFormat
    {
        private Dictionary<string, IOperation> mOperations = new();
        private Dictionary<string, IWeaponSystem> mSystems = new();
        private Dictionary<string, IInput> mInputs = new();

        public bool ParseDefinition(IFactory<IOperation> operationTypes, IFactory<IWeaponSystem> systemTypes, IFactory<IInput> inputTypes, JsonObject behaviourAttributes, CollectibleObject collectible)
        {
            foreach (JsonObject systemDefinition in behaviourAttributes["systems"].AsArray())
            {
                AddObject(systemDefinition, collectible, systemTypes, mSystems); 
            }

            foreach (JsonObject systemDefinition in behaviourAttributes["operations"].AsArray())
            {
                AddObject(systemDefinition, collectible, operationTypes, mOperations);
            }

            foreach (JsonObject systemDefinition in behaviourAttributes["inputs"].AsArray())
            {
                AddObject(systemDefinition, collectible, inputTypes, mInputs);
            }

            return true;
        }
        public Dictionary<string, IOperation> GetOperations()
        {
            return mOperations;
        }
        public Dictionary<string, IWeaponSystem> GetSystems()
        {
            return mSystems;
        }
        public Dictionary<string, IInput> GetInputs()
        {
            return mInputs;
        }

        static private void AddObject<ObjectInterface>(JsonObject definition, CollectibleObject collectible, IFactory<ObjectInterface> factory, Dictionary<string, ObjectInterface> container)
        {
            string objectCode = definition["code"].AsString();
            string objectClass = definition["class"].AsString();
            JsonObject atributes = definition["atributes"];
            ObjectInterface objectInstance = factory.Instantiate(objectClass, atributes, collectible);
            container.Add(objectCode, objectInstance);
        }
    }
}
