using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    public class ActiveSlotPassiveListener : IActiveSlotListener
    {
        private readonly ICoreClientAPI mClientApi;
        private readonly Dictionary<IActiveSlotListener.SlotEventType, Dictionary<int, System.Func<int, bool>>> mCallbacks = new();
        private int mLastListenerId = 0;

        private ItemSlot mLastSlot;
        private CollectibleObject mLastCollectible = null;
        private int? mLastItemsCount = null;

        public ActiveSlotPassiveListener(ICoreClientAPI api)
        {
            mClientApi = api;
            mClientApi.Event.RegisterGameTickListener(Listener, 0);

            mLastSlot = mClientApi.World?.Player?.Entity?.ActiveHandItemSlot;
            mLastCollectible = mLastSlot?.Itemstack?.Collectible;
            mLastItemsCount = mLastSlot?.Itemstack?.StackSize;
        }

        int IActiveSlotListener.RegisterListener(IActiveSlotListener.SlotEventType eventType, System.Func<int, bool> callback)
        {
            mLastListenerId++;
            mCallbacks.TryAdd(eventType, new());
            mCallbacks[eventType].Add(mLastListenerId, callback);
            return mLastListenerId;
        }

        void IActiveSlotListener.UnregisterListener(int id)
        {
            foreach (Dictionary<int, System.Func<int, bool>> listener in mCallbacks.Values.Where(listener => listener.ContainsKey(id)))
            {
                listener.Remove(id);
            }
        }

        private void Listener(float timePassed)
        {
            IActiveSlotListener.SlotEventType? eventType = CheckForChange();

            if (eventType == null || !mCallbacks.ContainsKey((IActiveSlotListener.SlotEventType)eventType)) return;

            foreach (var callback in mCallbacks[(IActiveSlotListener.SlotEventType)eventType].Values)
            {
                callback(GetSlotId());
            }
        }

        private int GetSlotId()
        {
            int? slotId = mClientApi?.World?.Player?.Entity?.GearInventory?.GetSlotId(mLastSlot);
            return slotId == null ? -1 : (int)slotId;
        }

        private IActiveSlotListener.SlotEventType? CheckForChange()
        {
            ItemSlot currentSlot = mClientApi.World.Player.Entity.ActiveHandItemSlot;
            CollectibleObject currentCollectible = mLastSlot?.Itemstack?.Collectible;
            int? currentItemsCount = mLastSlot?.Itemstack?.StackSize;

            if (currentSlot != mLastSlot)
            {
                mLastSlot = currentSlot;
                return null;
            }

            if (currentCollectible != mLastCollectible)
            {
                mLastCollectible = currentCollectible;
                return null;
            }

            if (currentItemsCount != mLastItemsCount)
            {
                mLastItemsCount = currentItemsCount;
                return IActiveSlotListener.SlotEventType.ItemDropped;
            }

            return null;
        }
    }

    public class ActiveSlotActiveListener : IActiveSlotListener
    {
        private readonly ICoreClientAPI mClientApi;
        private readonly Dictionary<IActiveSlotListener.SlotEventType, Dictionary<int, System.Func<int, bool>>> mCallbacks = new();
        private int mLastListenerId = 0;

        private readonly List<string> mHotkeys = new List<string>
        {
            "dropitem",
            "dropitems"
        };

        private readonly Dictionary<string, IActiveSlotListener.SlotEventType> mHotkeysToEventType = new()
        {
            { "dropitem",  IActiveSlotListener.SlotEventType.ItemDropped },
            { "dropitems",  IActiveSlotListener.SlotEventType.ItemDropped }
        };

        public ActiveSlotActiveListener(ICoreClientAPI api)
        {
            mClientApi = api;
            mClientApi.Event.KeyDown += KeyPressListener;
        }

        int IActiveSlotListener.RegisterListener(IActiveSlotListener.SlotEventType eventType, System.Func<int, bool> callback)
        {
            mLastListenerId++;
            mCallbacks.TryAdd(eventType, new());
            mCallbacks[eventType].Add(mLastListenerId, callback);
            return mLastListenerId;
        }

        void IActiveSlotListener.UnregisterListener(int id)
        {
            foreach (Dictionary<int, System.Func<int, bool>> listener in mCallbacks.Values.Where(listener => listener.ContainsKey(id)))
            {
                listener.Remove(id);
            }
        }

        private void KeyPressListener(KeyEvent ev)
        {
            foreach (string hotkeyId in mHotkeys)
            {
                if (!mClientApi.Input.HotKeys.ContainsKey(hotkeyId))
                {
                    mClientApi.Logger.Error("[Firearms] [ActiveSlotActiveListener] [KeyPressListener()] Hotkey '" + hotkeyId + "' not found");
                }

                if (mClientApi.Input.HotKeys.ContainsKey(hotkeyId) && CompareCombinations(ev, mClientApi.Input.HotKeys[hotkeyId].CurrentMapping))
                {
                    HotkeyPressHandler(hotkeyId, ev);
                    break;
                }
            }
        }

        private bool CompareCombinations(KeyEvent A, KeyCombination B)
        {
            if (A.KeyCode != B.KeyCode) return false;
            if (A.ShiftPressed != B.Shift) return false;
            if (A.CtrlPressed != B.Ctrl) return false;
            if (A.AltPressed != B.Alt) return false;

            return true;
        }

        private void HotkeyPressHandler(string hotkeyId, KeyEvent ev)
        {
            IActiveSlotListener.SlotEventType eventType = mHotkeysToEventType[hotkeyId];

            if (!mCallbacks.ContainsKey(eventType)) return;

            bool handled = false;
            foreach (var callback in mCallbacks[eventType].Values)
            {
                bool handledByCallback = callback(GetSlotId());
                if (handledByCallback) handled = true;
            }

            if (handled) ev.Handled = true;
        }

        private int GetSlotId()
        {
            int? slotId = mClientApi?.World?.Player?.InventoryManager?.GetHotbarInventory()?.GetSlotId(mClientApi.World.Player.Entity.ActiveHandItemSlot);
            return slotId == null ? -1 : (int)slotId;
        }
    }
}
