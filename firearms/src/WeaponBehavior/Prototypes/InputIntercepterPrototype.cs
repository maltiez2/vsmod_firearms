using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using static MaltiezFirearms.WeaponBehavior.IInputInterceptor;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    public class InputIntercepterPrototype : IInputInterceptor
    {
        private readonly ICoreClientAPI cApi;
        private readonly List<IInput> mInputs = new List<IInput>();
        private readonly List<InputCallback> mCallbacks = new List<InputCallback>();
        private readonly List<CollectibleObject> mCollectibles = new List<CollectibleObject>();
        private readonly InputPacketSender mPacketSender;

        private const string mNetworkChannelName = "maltiezfierarms_inputIntercepter";

        public InputIntercepterPrototype(ICoreAPI api)
        {
            mPacketSender = new InputPacketSender(api, ServerHotKeyProxyHandler, mNetworkChannelName);

            if (api.Side == EnumAppSide.Client)
            {
                cApi = api as ICoreClientAPI;
            }
        }

        public void RegisterInput(IInput input, InputCallback callback, CollectibleObject collectible)
        {
            int inputIndex = mInputs.Count;
            string inputCode = inputIndex.ToString() + "_" + input.GetName(); // @TODO remove duplicates from different weapons

            mInputs.Add(input);
            mCallbacks.Add(callback);
            mCollectibles.Add(collectible);

            Tuple<bool, bool, bool> AltCtrlShift = input.GetIfAltCtrlShiftPressed();
            GlKeys key = (GlKeys)Enum.Parse(typeof(GlKeys), input.GetKey());

            cApi?.Input.RegisterHotKey(inputCode, inputCode, key, HotkeyType.CharacterControls, AltCtrlShift.Item1, AltCtrlShift.Item2, AltCtrlShift.Item3);
            cApi?.Input.SetHotKeyHandler(inputCode, _ => ClientHotKeyProxyHandler(inputIndex));
        }

        public bool ClientHotKeyProxyHandler(int inputIndex)
        {
            EntityAgent player = cApi.World.Player.Entity;

            mPacketSender.SendPacket(inputIndex);

            return HandlerCaller(inputIndex, player);
        }

        public void ServerHotKeyProxyHandler(int inputIndex, IServerPlayer serverPlayer)
        {
            EntityAgent player = serverPlayer.Entity;

            HandlerCaller(inputIndex, player);
        }

        private bool HandlerCaller(int inputIndex, EntityAgent player)
        {
            ItemSlot slot = player.ActiveHandItemSlot;

            if (slot.Itemstack.Collectible != mCollectibles[inputIndex])
            {
                return false;
            }

            IInput input = mInputs[inputIndex];
            InputCallback handler = mCallbacks[inputIndex];

            return handler(slot, player, input);
        }
    }

    public class InputPacketSender
    {
        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        public class InputPacket
        {
            public int inputIndex;
        }

        public delegate void InputHandler(int inputIndex, IServerPlayer player);
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
            mHandler(packet.inputIndex, fromPlayer);
        }

        // CLIENT SIDE

        IClientNetworkChannel clientNetworkChannel;

        private void StartClientSide(ICoreClientAPI api, string channelName)
        {
            clientNetworkChannel = api.Network.RegisterChannel(channelName)
            .RegisterMessageType<InputPacket>();
        }

        public void SendPacket(int index)
        {
            clientNetworkChannel.SendPacket(new InputPacket()
            {
                inputIndex = index
            });
        }
    }
}
