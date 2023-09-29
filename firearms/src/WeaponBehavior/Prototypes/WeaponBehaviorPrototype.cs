using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.WeaponBehavior.IInputInterceptor;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    public class WeaponBehaviorPrototype : CollectibleBehavior
    {
        public WeaponBehaviorPrototype(CollectibleObject collObj) : base(collObj)
        {

        }

        private ICoreAPI mApi;
        private IFactoryProvider mFactories;
        private IFiniteStateMachine mFsm;
        private IInputInterceptor mInputIterceptor;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            mApi = api;
            mFactories = mApi.ModLoader.GetModSystem<WeaponBehaviorSystem>();
            mInputIterceptor = mApi.ModLoader.GetModSystem<WeaponBehaviorSystem>().GetInputInterceptor();
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            IBehaviourFormat mParser = new BehaviourFormatPrototype();
            mParser.ParseDefinition(mFactories.GetOperationFactory(), mFactories.GetSystemFactory(), mFactories.GetInputFactory(), properties, collObj);

            mFsm = new FsmPrototype();
            mFsm.Init(mApi, mParser.GetOperations(), mParser.GetSystems(), mParser.GetInputs(), properties, collObj);

            foreach (var inputEntry in mParser.GetInputs())
            {
                mInputIterceptor.RegisterInput(inputEntry.Value, mFsm.Process, collObj);
            }
        }
    }
}
