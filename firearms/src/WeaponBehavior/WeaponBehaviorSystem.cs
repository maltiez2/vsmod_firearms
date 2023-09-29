using firearms.src;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using MaltiezFirearms.WeaponBehavior.Prototypes;

namespace MaltiezFirearms.WeaponBehavior
{
    public class WeaponBehaviorSystem : ModSystem, IFactoryProvider
    {
        private ICoreAPI mApi;
        private IFactory<IOperation> mOperationFactory;
        private IFactory<IWeaponSystem> mSystemFactory;
        private IFactory<IInput> mInputFactory;
        IInputInterceptor mInputIterceptor;


        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            mApi = api;

            api.RegisterCollectibleBehaviorClass("maltiezfirearms.weapon", typeof(WeaponBehaviorPrototype));

            mOperationFactory = new FactoryPrototype<IOperation>();
            mSystemFactory = new FactoryPrototype<IWeaponSystem>();
            mInputFactory = new FactoryPrototype<IInput>();

            mOperationFactory.RegisterType<OperationPrototype>("TestOperation");
            mSystemFactory.RegisterType<WeaponSystemPrototype>("TestSystem");
            mInputFactory.RegisterType<InputPrototype>("TestInput");

            mInputIterceptor = new InputIntercepterPrototype(mApi);
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
