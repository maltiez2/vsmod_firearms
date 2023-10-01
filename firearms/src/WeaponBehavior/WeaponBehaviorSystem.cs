using MaltiezFirearms.WeaponBehavior.Inputs;
using MaltiezFirearms.WeaponBehavior.Operations;
using Vintagestory.API.Common;

namespace MaltiezFirearms.WeaponBehavior
{
    public class WeaponBehaviorSystem : ModSystem, IFactoryProvider
    {
        private IFactory<IOperation> mOperationFactory;
        private IFactory<IWeaponSystem> mSystemFactory;
        private IFactory<IInput> mInputFactory;
        private IInputInterceptor mInputIterceptor;


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterCollectibleBehaviorClass("maltiezfirearms.weapon", typeof(Prototypes.WeaponBehaviorPrototype));

            mOperationFactory = new Prototypes.FactoryPrototype<IOperation, UniqueIdGeneratorForFactory>();
            mSystemFactory = new Prototypes.FactoryPrototype<IWeaponSystem, UniqueIdGeneratorForFactory>();
            mInputFactory = new Prototypes.FactoryPrototype<IInput, UniqueIdGeneratorForFactory>();

            RegisterSystems();
            RegisterOperations();
            RegisterInputs();

            mInputIterceptor = new Prototypes.InputIntercepterPrototype(api);
        }

        public void RegisterSystems()
        {  
            mSystemFactory.RegisterType<Systems.BasicSoundSystem>("Sound");
        }
        public void RegisterOperations()
        {
            mOperationFactory.RegisterType<SimpleOperation>("Simple");
        }

        public void RegisterInputs()
        {
            mInputFactory.RegisterType<SimpleKeyPress>("SimpleKeyPress");
        }

        public IFactory<IOperation> GetOperationFactory()
        {
            return mOperationFactory;
        }
        public IFactory<IWeaponSystem> GetSystemFactory()
        {
            return mSystemFactory;
        }
        public IFactory<IInput> GetInputFactory()
        {
            return mInputFactory;
        }
        public IInputInterceptor GetInputInterceptor()
        {
            return mInputIterceptor;
        }
    }
}
