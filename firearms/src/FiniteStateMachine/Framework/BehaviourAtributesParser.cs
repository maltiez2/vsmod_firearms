using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    public class BehaviourAttributesParser : IBehaviourAttributesParser
    {
        public const string CodeAttrName = "code";
        public const string ClassAttrName = "class";
        public const string AttributesAttrName = "attributes";
        
        private readonly Dictionary<string, IOperation> mOperations = new();
        private readonly Dictionary<string, ISystem> mSystems = new();
        private readonly Dictionary<string, IInput> mInputs = new();

        public bool ParseDefinition(IFactory<IOperation> operationTypes, IFactory<ISystem> systemTypes, IFactory<IInput> inputTypes, JsonObject behaviourAttributes, CollectibleObject collectible)
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
        public Dictionary<string, ISystem> GetSystems()
        {
            return mSystems;
        }
        public Dictionary<string, IInput> GetInputs()
        {
            return mInputs;
        }

        static private void AddObject<ObjectInterface>(JsonObject definition, CollectibleObject collectible, IFactory<ObjectInterface> factory, Dictionary<string, ObjectInterface> container)
        {
            string objectCode = definition[CodeAttrName].AsString();
            string objectClass = definition[ClassAttrName].AsString();
            JsonObject attributes = definition[AttributesAttrName];
            ObjectInterface objectInstance = factory.Instantiate(objectClass, attributes, collectible);
            container.Add(objectCode, objectInstance);
        }
    }
}
