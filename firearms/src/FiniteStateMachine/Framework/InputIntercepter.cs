using ProtoBuf;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using static MaltiezFirearms.FiniteStateMachine.API.IInputInterceptor;
using static MaltiezFirearms.FiniteStateMachine.API.IKeyInput;
using static MaltiezFirearms.FiniteStateMachine.API.IMouseInput;
using static MaltiezFirearms.FiniteStateMachine.API.ISlotChanged;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    public class InputIntercepter : IInputInterceptor
    {
        private readonly ICoreClientAPI cApi;
        private readonly List<IInput> mInputs = new List<IInput>();
        private readonly List<InputCallback> mCallbacks = new List<InputCallback>();
        private readonly List<CollectibleObject> mCollectibles = new List<CollectibleObject>();
        private readonly InputPacketSender mPacketSender;

        private const string mNetworkChannelName = "maltiezfierarms_inputIntercepter";

        public InputIntercepter(ICoreAPI api)
        {
            mPacketSender = new InputPacketSender(api, ServerInputProxyHandler, mNetworkChannelName);

            if (api.Side == EnumAppSide.Client)
            {
                cApi = api as ICoreClientAPI;
            }
        }

        public void RegisterInput(IInput input, InputCallback callback, CollectibleObject collectible)
        {
            int inputIndex = mInputs.Count;

            mInputs.Add(input);
            mCallbacks.Add(callback);
            mCollectibles.Add(collectible);

            if (input is IHotkeyInput && cApi != null)
            {
                string inputCode = inputIndex.ToString() + "_" + input.GetName(); // @TODO remove duplicates from different weapons
                ClientRegisterHotkey(input as IHotkeyInput, inputCode, inputIndex);
            }

            if (input is IEventInput && cApi != null)
            {
                ClientRegisterEventHandler(input as IEventInput, inputIndex);
            }
        }
        private void ClientRegisterHotkey(IHotkeyInput input, string inputCode, int inputIndex)
        {
            KeyPressModifiers AltCtrlShift = input.GetIfAltCtrlShiftPressed();
            GlKeys key = (GlKeys)Enum.Parse(typeof(GlKeys), input.GetKey());

            cApi.Input.RegisterHotKey(inputCode, inputCode, key, HotkeyType.CharacterControls, AltCtrlShift.Alt, AltCtrlShift.Ctrl, AltCtrlShift.Shift);
            cApi.Input.SetHotKeyHandler(inputCode, _ => ClientInputProxyHandler(inputIndex, null));
        }
        private void ClientRegisterEventHandler(IEventInput input, int inputIndex)
        {
            switch ((input as IKeyInput)?.GetEventType())
            {
                case KeyEventType.KeyDown:
                    cApi.Event.KeyDown += (KeyEvent ev) => ClientKeyInputProxyHandler(ev, inputIndex, KeyEventType.KeyDown);
                    break;
                case KeyEventType.KeyUp:
                    cApi.Event.KeyUp += (KeyEvent ev) => ClientKeyInputProxyHandler(ev, inputIndex, KeyEventType.KeyUp);
                    break;
                case null:
                    break;
            }

            switch ((input as IMouseInput)?.GetEventType())
            {
                case MouseEventType.MouseDown:
                    cApi.Event.MouseDown += (MouseEvent ev) => ClientMouseInputProxyHandler(ev, inputIndex, MouseEventType.MouseDown);
                    break;
                case MouseEventType.MouseUp:
                    cApi.Event.MouseUp += (MouseEvent ev) => ClientMouseInputProxyHandler(ev, inputIndex, MouseEventType.MouseUp);
                    break;
                case MouseEventType.MouseMove:
                    cApi.Event.MouseMove += (MouseEvent ev) => ClientMouseInputProxyHandler(ev, inputIndex, MouseEventType.MouseMove);
                    break;
                case null:
                    break;
            }

            if (input is ISlotChanged)
            {
                 cApi.Event.AfterActiveSlotChanged += (ActiveSlotChangeEventArgs ev) => ClientSlotInputProxyHandler(inputIndex, ev.FromSlot, ev.ToSlot);
            }
        }

        private bool ClientCheckModifiers(KeyPressModifiers modifiers)
        {
            bool altPressed = cApi.Input.KeyboardKeyState[(int)GlKeys.AltLeft] || cApi.Input.KeyboardKeyState[(int)GlKeys.AltRight];
            bool ctrlPressed = cApi.Input.KeyboardKeyState[(int)GlKeys.ControlLeft] || cApi.Input.KeyboardKeyState[(int)GlKeys.ControlRight];
            bool shiftPressed = cApi.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] || cApi.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];

            return modifiers.Alt == altPressed && modifiers.Ctrl == ctrlPressed && modifiers.Shift == shiftPressed;
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
                player = cApi?.World?.Player;
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
            IKeyInput input = mInputs[inputIndex] as IKeyInput;
            GlKeys key = (GlKeys)Enum.Parse(typeof(GlKeys), input.GetKey());
            KeyPressModifiers modifiers = input.GetIfAltCtrlShiftPressed();

            if (input.GetEventType() != keyEventType) return false;
            if (ev.KeyCode != (int)key) return false;
            if (modifiers.Alt != ev.AltPressed || modifiers.Ctrl != ev.CtrlPressed || modifiers.Shift != ev.ShiftPressed) return false;

            return ClientInputProxyHandler(inputIndex, null);
        }
        public bool ClientMouseInputProxyHandler(MouseEvent ev, int inputIndex, MouseEventType keyEventType)
        {
            IMouseInput input = mInputs[inputIndex] as IMouseInput;
            EnumMouseButton key = (EnumMouseButton)Enum.Parse(typeof(EnumMouseButton), input.GetKey());
            KeyPressModifiers modifiers = input.GetIfAltCtrlShiftPressed();

            if (input.GetEventType() != keyEventType) return false;
            if (ev.Button != key) return false;
            if (!ClientCheckModifiers(modifiers)) return false;

            return ClientInputProxyHandler(inputIndex, null);
        }
        public bool ClientSlotInputProxyHandler(int inputIndex, int fromSlotId, int toSlotId)
        {
            ISlotChanged input = mInputs[inputIndex] as ISlotChanged;

            int slotId;
            if (input.GetEventType() == SlotEventType.FromWeapon)
            {
                slotId = fromSlotId;
            }
            else if (input.GetEventType() == SlotEventType.ToWeapon)
            {
                slotId = toSlotId;
            }
            else
            {
                throw new NotImplementedException();
            }

            return ClientInputProxyHandler(inputIndex, slotId);
        }
        public bool ClientInputProxyHandler(int inputIndex, int? slotId)
        {
            EntityAgent playerEntity = cApi.World.Player.Entity;

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
            if (slot.Itemstack.Collectible != mCollectibles[inputIndex])
            {
                return false;
            }

            IInput input = mInputs[inputIndex];
            InputCallback callback = mCallbacks[inputIndex];

            return callback(slot, player, input);
        }
    }

    public class InputPacketSender
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class InputPacket
        {
            public int inputIndex;
            public int? slotId;
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
            mHandler(packet.inputIndex, packet.slotId, fromPlayer);
        }

        // CLIENT SIDE

        IClientNetworkChannel clientNetworkChannel;

        private void StartClientSide(ICoreClientAPI api, string channelName)
        {
            clientNetworkChannel = api.Network.RegisterChannel(channelName)
            .RegisterMessageType<InputPacket>();
        }
        public void SendPacket(int index, int? slot)
        {
            clientNetworkChannel.SendPacket(new InputPacket()
            {
                inputIndex = index,
                slotId = slot
            });
        }
    }
}
