using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using static MaltiezFirearms.WeaponBehavior.Prototypes.FsmPrototype;

namespace MaltiezFirearms.WeaponBehavior
{
    public interface IAimingBehavior
    {
        Tuple<Vec3f, Vec3f> GetShootingDirectionAndPosition();
    }

    public interface IState
    {
    }

    public interface IFactoryObject
    {
        void Init(TreeAttribute definition, CollectibleObject colelctible);
    }
    public interface IOperation : IFactoryObject
    {
        IState Perform(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input);
        int? Timer(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input);
        
        List<string> GetStates();
        List<string> GetInputs();
        void SetSystems(Dictionary<string, IWeaponSystem> systems);
        void SetStates(Dictionary<string, IState> states);
        void SetInputs(Dictionary<string, IInput> inputs);
    }
    public interface IWeaponSystem : IFactoryObject
    {
        bool Verify(ItemSlot weaponSlot, EntityAgent player, TreeAttribute parameters);
        bool Process(ItemSlot weaponSlot, EntityAgent player, TreeAttribute parameters);
    }
    public interface IInput : IFactoryObject
    {
        enum InputType
        {
            CLICK,
            HOLD
        }
        string GetName();
        InputType GetInputType();
        Tuple<bool, bool, bool> GetIfAltCtrlShiftPressed();
        string GetKey();
    }
    
    public interface IFactory<ProducedClass>
    {
        Type GetType(string name);
        void RegisterType<ObjectClass>(string name) where ObjectClass : ProducedClass, new();
        ProducedClass Instantiate(string name, TreeAttribute definition, CollectibleObject colelctible);
    }
    public interface IBehaviourFormat
    {
        bool ParseDefinition(IFactory<IOperation> operationTypes, IFactory<IWeaponSystem> systemTypes, IFactory<IInput> inputTypes, JsonObject behaviourAttributes, CollectibleObject collectible);
        Dictionary<string, IOperation> GetOperations();
        Dictionary<string, IWeaponSystem> GetSystems();
        Dictionary<string, IInput> GetInputs();
    }
    public interface IFiniteStateMachine
    {
        void Init(ICoreAPI api, Dictionary<string, IOperation> operations, Dictionary<string, IWeaponSystem> systems, Dictionary<string, IInput> inputs, JsonObject behaviourAttributes, CollectibleObject colelctible);
        bool Process(ItemSlot weaponSlot, EntityAgent player, IInput input);
    }

    public interface IInputInterceptor
    {
        public delegate bool InputCallback(ItemSlot weaponSlot, EntityAgent player, IInput input);
        void RegisterInput(IInput input, InputCallback callback, CollectibleObject collectible);
    }
    public interface IFactoryProvider
    {
        IFactory<IOperation> GetOperationFactory();
        IFactory<IWeaponSystem> GetSystemFactory();
        IFactory<IInput> GetInputFactory();
    }

}
