using System;
using System.Collections.Generic;
using System.Linq;
using HydrateOrDiedrate.FluidNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HydrateorDiedrate.FluidNetwork
{
    public class FluidNetworkPacket
    {
        public long networkId;
        public float totalFluid;
        public float averagePressure;
    }

    public class FluidClientRequestPacket
    {
        public long networkId;
    }

    public class FluidNetworkRemovedPacket
    {
        public long networkId;
    }

    public class FluidModSystem : ModSystem, IRenderer, IDisposable
    {
        public ICoreAPI Api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        IClientNetworkChannel clientNw;
        IServerNetworkChannel serverNw;

        public double RenderOrder => 0;
        public int RenderRange => 9999;

        public long TickNumber => data.tickNumber;

        FluidData data = new FluidData();
        bool allNetworksFullyLoaded = true;
        List<HydrateOrDiedrate.FluidNetwork.FluidNetwork> nowFullyLoaded = new List<HydrateOrDiedrate.FluidNetwork.FluidNetwork>();

        class FluidData
        {
            public long nextNetworkId = 1;
            public long tickNumber = 0;
            public Dictionary<long, HydrateOrDiedrate.FluidNetwork.FluidNetwork> networksById = new Dictionary<long, HydrateOrDiedrate.FluidNetwork.FluidNetwork>();
        }

        public override bool ShouldLoad(EnumAppSide side) => true;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            Api = api;

            if (api.World is IClientWorldAccessor)
            {
                var c = (ICoreClientAPI)api;
                c.Event.RegisterRenderer(this, EnumRenderStage.Before, "fluidnettick");
                clientNw = c.Network.RegisterChannel("fluidnetwork")
                    .RegisterMessageType(typeof(FluidNetworkPacket))
                    .RegisterMessageType(typeof(FluidNetworkRemovedPacket))
                    .RegisterMessageType(typeof(FluidClientRequestPacket))
                    .SetMessageHandler<FluidNetworkPacket>(OnPacket)
                    .SetMessageHandler<FluidNetworkRemovedPacket>(OnNetworkRemoved);
            }
            else
            {
                sapi = (ICoreServerAPI)api;
                api.World.RegisterGameTickListener(OnServerTick, 20);
                serverNw = sapi.Network.RegisterChannel("fluidnetwork")
                    .RegisterMessageType(typeof(FluidNetworkPacket))
                    .RegisterMessageType(typeof(FluidNetworkRemovedPacket))
                    .RegisterMessageType(typeof(FluidClientRequestPacket))
                    .SetMessageHandler<FluidClientRequestPacket>(OnClientRequest);
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            base.StartServerSide(api);
            api.Event.SaveGameLoaded += () => data = new FluidData();
            api.Event.ChunkDirty += Event_ChunkDirty;
        }

        void OnServerTick(float dt)
        {
            data.tickNumber++;
            // BEFORE:
            // foreach (var nw in data.networksById.Values.ToList())
            //     if (nw.fullyLoaded && nw.nodes.Count > 0) nw.ServerTick(dt, data.tickNumber);

            // AFTER: tick any network that has nodes
            foreach (var nw in data.networksById.Values.ToList())
            {
                if (nw.nodes.Count > 0)
                {
                    nw.ServerTick(dt, data.tickNumber);
                }
            }
        }

        void OnPacket(FluidNetworkPacket msg)
        {
            var nw = GetOrCreateNetwork(msg.networkId);
            bool isNew = !nw.clientSeen;
            nw.clientSeen = true;
            nw.UpdateFromPacket(msg, isNew);
        }

        void OnNetworkRemoved(FluidNetworkRemovedPacket msg)
        {
            data.networksById.Remove(msg.networkId);
        }

        void OnClientRequest(IServerPlayer player, FluidClientRequestPacket msg)
        {
            if (data.networksById.TryGetValue(msg.networkId, out var nw))
            {
                nw.SendBlocksUpdateToClient(player);
            }
        }

        public void Broadcast(FluidNetworkPacket packet)
        {
            serverNw?.BroadcastPacket(packet);
        }

        public HydrateOrDiedrate.FluidNetwork.FluidNetwork CreateNetwork(FluidInterfaces.IFluidDevice producerNode)
        {
            var nw = new HydrateOrDiedrate.FluidNetwork.FluidNetwork(this, data.nextNetworkId);
            nw.fullyLoaded = true;
            data.networksById[data.nextNetworkId] = nw;
            data.nextNetworkId++;
            return nw;
        }

        public void DeleteNetwork(HydrateOrDiedrate.FluidNetwork.FluidNetwork nw)
        {
            data.networksById.Remove(nw.networkId);
            serverNw?.BroadcastPacket(new FluidNetworkRemovedPacket { networkId = nw.networkId });
        }

        public HydrateOrDiedrate.FluidNetwork.FluidNetwork GetOrCreateNetwork(long id)
        {
            if (!data.networksById.TryGetValue(id, out var nw))
            {
                nw = data.networksById[id] = new HydrateOrDiedrate.FluidNetwork.FluidNetwork(this, id);
            }
            TestFullyLoaded(nw);
            return nw;
        }

        public void TestFullyLoaded(HydrateOrDiedrate.FluidNetwork.FluidNetwork nw)
        {
            if (Api.Side != EnumAppSide.Server || nw.fullyLoaded) return;
            nw.fullyLoaded = nw.TestFullyLoaded(Api);
            allNetworksFullyLoaded &= nw.fullyLoaded;
        }

        void Event_ChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
        {
            if (allNetworksFullyLoaded || reason == EnumChunkDirtyReason.MarkedDirty) return;

            allNetworksFullyLoaded = true;
            nowFullyLoaded.Clear();

            foreach (var nw in data.networksById.Values)
            {
                if (!nw.fullyLoaded)
                {
                    allNetworksFullyLoaded = false;
                    if (nw.inChunks.ContainsKey(chunkCoord))
                    {
                        TestFullyLoaded(nw);
                        if (nw.fullyLoaded) nowFullyLoaded.Add(nw);
                    }
                }
            }
            for (int i = 0; i < nowFullyLoaded.Count; i++)
            {
                // (Re)discover to ensure correct multi-exit links
                nowFullyLoaded[i].Rebuild(null);
            }
        }

        public void OnRenderFrame(float dt, EnumRenderStage stage)
        {
            if (capi?.IsGamePaused == true) return;
            foreach (var nw in data.networksById.Values) nw.ClientTick(dt);
        }

        public override void Dispose() { base.Dispose(); }
    }
}
