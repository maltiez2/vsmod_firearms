using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Inputs
{
    public class ItemDropped : BaseInput, ISlotEvent
    {
        IActiveSlotListener.SlotEventType ISlotEvent.GetEventType()
        {
            return IActiveSlotListener.SlotEventType.ItemDropped;
        }
    }
}
