using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace MaltiezFirearms.WeaponBehavior
{
    public struct KeyPressModifiers
    {
        public bool Alt;
        public bool Ctrl;
        public bool Shift;

        public KeyPressModifiers(bool alt, bool ctrl, bool shift)
        {
            Alt = alt;
            Ctrl = ctrl;
            Shift = shift;
        }
    }
    
    public interface IAimingBehavior
    {
        Tuple<Vec3f, Vec3f> GetShootingDirectionAndPosition();
    }

    public interface IState
    {
    }

    public interface IFactoryObject
    {
        void Init(JsonObject definition, CollectibleObject colelctible);
        void SetId(int id);
        int GetId();
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
        bool Verify(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters);
        bool Process(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters);
    }
    public interface IKeyRelatedInput
    {
        KeyPressModifiers GetIfAltCtrlShiftPressed();
        string GetKey();
    }
    public interface IInput : IFactoryObject
    {
        string GetName();

    }
    public interface IHotkeyInput : IInput, IKeyRelatedInput
    {
        
    }
    public interface IEventInput : IInput
    {

    }
    public interface IKeyInput : IEventInput, IKeyRelatedInput
    {
        enum KeyEventType
        {
            KeyDown,
            KeyUp
        }
        KeyEventType GetEventType();
    }
    public interface IMouseInput : IEventInput, IKeyRelatedInput
    {
        enum MouseEventType
        {
            MouseMove,
            MouseDown,
            MouseUp
        }
        MouseEventType GetEventType();
    }
    public interface ISlotChanged : IEventInput
    {
        enum SlotEventType
        {
            ToWeapon,
            FromWeapon
        }
        SlotEventType GetEventType();
    }


    public interface IFactory<ProducedClass>
    {
        Type GetType(string name);
        void RegisterType<ObjectClass>(string name) where ObjectClass : ProducedClass, new();
        ProducedClass Instantiate(string name, JsonObject definition, CollectibleObject colelctible);
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
    public interface IUniqueIdGeneratorForFactory
    {
        int GenerateInstanceId();
        int GetFacrotyid();
    }
}
