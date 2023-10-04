using ProtoBuf;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using static MaltiezFirearms.FiniteStateMachine.API.IInputManager;
using static MaltiezFirearms.FiniteStateMachine.API.IKeyInput;
using static MaltiezFirearms.FiniteStateMachine.API.IMouseInput;
using static MaltiezFirearms.FiniteStateMachine.API.ISlotChangedAfter;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    public class InputManager : IInputManager
    {
        private readonly ICoreClientAPI mClientApi;
        private readonly IActiveSlotListener mSlotListener;
        private readonly List<IInput> mInputs = new();
        private readonly List<InputCallback> mCallbacks = new();
        private readonly List<CollectibleObject> mCollectibles = new();
        private readonly InputPacketSender mPacketSender;

        private const string cNetworkChannelName = "maltiezfierarms.inputManager";

        // Whitelisted GuiDialogs
        private readonly static Type rHudMouseToolsType = typeof(Vintagestory.Client.NoObf.ClientMain).Assembly.GetType("Vintagestory.Client.NoObf.HudMouseTools");

        public InputManager(ICoreAPI api, IActiveSlotListener slotListener)
        {
            mPacketSender = new InputPacketSender(api, ServerInputProxyHandler, cNetworkChannelName);

            if (api.Side == EnumAppSide.Client)
            {
                mClientApi = api as ICoreClientAPI;
                mSlotListener = slotListener;
            }
        }

        public void RegisterInput(IInput input, InputCallback callback, CollectibleObject collectible)
        {
            int inputIndex = mInputs.Count;

            mInputs.Add(input);
            mCallbacks.Add(callback);
            mCollectibles.Add(collectible);

            if (input is IHotkeyInput && mClientApi != null)
            {
                string inputCode = inputIndex.ToString() + "_" + input.GetName(); // @TODO remove duplicates from different weapons
                ClientRegisterHotkey(input as IHotkeyInput, inputCode, inputIndex);
            }

            if (input is IEventInput && mClientApi != null)
            {
                ClientRegisterEventHandler(input as IEventInput, inputIndex);
            }
        }
        private void ClientRegisterHotkey(IHotkeyInput input, string inputCode, int inputIndex)
        {
            KeyPressModifiers altCtrlShift = input.GetIfAltCtrlShiftPressed();
            GlKeys key = (GlKeys)Enum.Parse(typeof(GlKeys), input.GetKey());

            mClientApi.Input.RegisterHotKey(inputCode, inputCode, key, HotkeyType.CharacterControls, altCtrlShift.Alt, altCtrlShift.Ctrl, altCtrlShift.Shift);
            mClientApi.Input.SetHotKeyHandler(inputCode, _ => ClientInputProxyHandler(inputIndex, null));
        }
        private void ClientRegisterEventHandler(IEventInput input, int inputIndex)
        {
            switch ((input as IKeyInput)?.GetEventType())
            {
                case KeyEventType.KEY_DOWN:
                    mClientApi.Event.KeyDown += (KeyEvent ev) => ClientKeyInputProxyHandler(ev, inputIndex, KeyEventType.KEY_DOWN);
                    break;
                case KeyEventType.KEY_UP:
                    mClientApi.Event.KeyUp += (KeyEvent ev) => ClientKeyInputProxyHandler(ev, inputIndex, KeyEventType.KEY_UP);
                    break;
                case null:
                    break;
            }

            switch ((input as IMouseInput)?.GetEventType())
            {
                case MouseEventType.MOUSE_DOWN:
                    mClientApi.Event.MouseDown += (MouseEvent ev) => ClientMouseInputProxyHandler(ev, inputIndex, MouseEventType.MOUSE_DOWN);
                    break;
                case MouseEventType.MOUSE_UP:
                    mClientApi.Event.MouseUp += (MouseEvent ev) => ClientMouseInputProxyHandler(ev, inputIndex, MouseEventType.MOUSE_UP);
                    break;
                case MouseEventType.MOUSE_MOVE:
                    mClientApi.Event.MouseMove += (MouseEvent ev) => ClientMouseInputProxyHandler(ev, inputIndex, MouseEventType.MOUSE_MOVE);
                    break;
                case null:
                    break;
            }

            if (input is ISlotChangedAfter)
            {
                 mClientApi.Event.AfterActiveSlotChanged += (ActiveSlotChangeEventArgs ev) => ClientSlotInputProxyHandler(inputIndex, ev.FromSlot, ev.ToSlot);
            }
            if (input is ISlotChangedBefore)
            {
                mClientApi.Event.BeforeActiveSlotChanged += (ActiveSlotChangeEventArgs ev) => ClientBeforeSlotInputProxyHandler(inputIndex, ev.FromSlot);
            }
            if (input is ISlotEvent)
            {
                mSlotListener.RegisterListener((input as ISlotEvent).GetEventType(), (int slotId) => ClientInputProxyHandler(inputIndex, slotId));
            }
        }

        private ItemSlot GetSlotById(int? slotId, IServerPlayer serverPlayer)
        {
            IPlayer player;
            
            if (serverPlayer != null)
            {
                player = serverPlayer;
            }
            else
            {
                player = mClientApi?.World?.Player;
            }
            
            if (slotId != null)
            {
                return player?.InventoryManager.GetHotbarInventory()[(int)slotId];
            }
            else
            {
                return player?.Entity.ActiveHandItemSlot;
            }
        }

        public bool ClientKeyInputProxyHandler(KeyEvent ev, int inputIndex, KeyEventType keyEventType)
        {
            if (!(mInputs[inputIndex] as IKeyInput).CheckIfShouldBeHandled(ev, keyEventType)) return false;

            if (!ClientIfEventShouldBeHandled()) return false;

            bool handled = ClientInputProxyHandler(inputIndex, null);
            if (handled) ev.Handled = true;
            
            return handled;
        }
        public bool ClientMouseInputProxyHandler(MouseEvent ev, int inputIndex, MouseEventType keyEventType)
        {
            if (!(mInputs[inputIndex] as IMouseInput).CheckIfShouldBeHandled(ev, keyEventType)) return false;

            if (!ClientIfEventShouldBeHandled()) return false;

            bool handled = ClientInputProxyHandler(inputIndex, null);
            if (handled) ev.Handled = true;
            
            return handled;
        }
        public bool ClientSlotInputProxyHandler(int inputIndex, int fromSlotId, int toSlotId)
        {
            switch ((mInputs[inputIndex] as ISlotChangedAfter).GetEventType())
            {
                case SlotEventType.FROM_WEAPON:
                    return ClientInputProxyHandler(inputIndex, fromSlotId);
                case SlotEventType.TO_WEAPON:
                    return ClientInputProxyHandler(inputIndex, toSlotId);
                default:
                    throw new NotImplementedException();
            }
        }
        public EnumHandling ClientBeforeSlotInputProxyHandler(int inputIndex, int fromSlotId)
        {
            if (ClientInputProxyHandler(inputIndex, fromSlotId))
            {
                return (mInputs[inputIndex] as ISlotChangedBefore).GetHandlingType();
            }

            return EnumHandling.PassThrough;
        }
        public bool ClientInputProxyHandler(int inputIndex, int? slotId)
        {
            EntityAgent playerEntity = mClientApi.World.Player.Entity;

            mPacketSender.SendPacket(inputIndex, slotId);

            return InputHandler(inputIndex, playerEntity, GetSlotById(slotId, null));
        }
        public void ServerInputProxyHandler(int inputIndex, int? slotId, IServerPlayer serverPlayer)
        {
            EntityAgent playerEntity = serverPlayer.Entity;

            InputHandler(inputIndex, playerEntity, GetSlotById(slotId, serverPlayer));
        }

        private bool InputHandler(int inputIndex, EntityAgent player, ItemSlot slot)
        {
            if (slot?.Itemstack?.Collectible == null || slot.Itemstack.Collectible != mCollectibles[inputIndex])
            {
                return false;
            }

            IInput input = mInputs[inputIndex];
            InputCallback callback = mCallbacks[inputIndex];
            bool handled = callback(slot, player, input);

            if (handled) mClientApi?.Logger.Error("[Firearms] [InputManager] Handling: " + input.GetName());

            return handled;
        }

        private bool ClientIfEventShouldBeHandled()
        {
            foreach (GuiDialog item in mClientApi.Gui.OpenedGuis)
            {
                if (item is HudElement) continue;
                if (item.GetType().IsAssignableFrom(rHudMouseToolsType)) continue;

                return false;
            }

            if (mClientApi.IsGamePaused)
            {
                return false;
            }

            return true;
        }
    }

    public class InputPacketSender
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class InputPacket
        {
            public int InputIndex;
            public int? SlotId;
        }

        public delegate void InputHandler(int inputIndex, int? slotId, IServerPlayer player);
        private InputHandler mHandler;
        public InputPacketSender(ICoreAPI api, InputHandler handler, string channelName)
        {
            if (api.Side == EnumAppSide.Server)
            {
                StartServerSide(api as ICoreServerAPI, handler, channelName);
            }
            else if (api.Side == EnumAppSide.Client)
            {
                StartClientSide(api as ICoreClientAPI, channelName);
            }
        }

        // SERVER SIDE

        private void StartServerSide(ICoreServerAPI api, InputHandler handler, string channelName)
        {
            mHandler = handler;
            api.Network.RegisterChannel(channelName)
            .RegisterMessageType<InputPacket>()
            .SetMessageHandler<InputPacket>(OnServerPacket);
        }
        private void OnServerPacket(IServerPlayer fromPlayer, InputPacket packet)
        {
            mHandler(packet.InputIndex, packet.SlotId, fromPlayer);
        }

        // CLIENT SIDE

        IClientNetworkChannel mClientNetworkChannel;

        private void StartClientSide(ICoreClientAPI api, string channelName)
        {
            mClientNetworkChannel = api.Network.RegisterChannel(channelName)
            .RegisterMessageType<InputPacket>();
        }
        public void SendPacket(int index, int? slot)
        {
            mClientNetworkChannel.SendPacket(new InputPacket()
            {
                InputIndex = index,
                SlotId = slot
            });
        }
    }
}
