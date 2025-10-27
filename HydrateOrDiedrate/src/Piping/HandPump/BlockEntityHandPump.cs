using System;
using System.Collections.Generic;
using System.IO;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Piping.Networking;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class BlockEntityHandPump : BlockEntityOpenableContainer
    {
        public const float PumpRateLitresPerSec = 1.25f;
        public const float IntakeLitresPerSec = 8f;

        public IPlayer PumpingPlayer { get; private set; }
        public override string InventoryClassName => "handpump";
        public override InventoryBase Inventory { get; }
        public ItemSlot ContainerSlot => Inventory[0];

        private float pendingDrawLitres;
        private float pendingIntakeLitres;
        public float lastSecondsUsed;
        private const float StrokePeriodSec = 2f;
        private const float LitresPerProductiveStroke = 2f;
        private bool strokeInProgress;
        private static bool SamePos(BlockPos a, BlockPos b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        private float strokeTimer;
        private int remainingPrimingStrokes;
        private bool willEmitParticlesThisStroke;
        private int  willExtractLitresThisStroke;
        private ILoadedSound pumpSound;
        private readonly AssetLocation pumpSfx = new AssetLocation("hydrateordiedrate", "sounds/pump1.ogg");
        private float particleAcc;
        private bool pumping;
        private HandPumpContainerRenderer containerRenderer;
        private BEBehaviorHandPumpAnim AnimBh => this.GetBehavior<BEBehaviorHandPumpAnim>();
        public BlockEntityHandPump()
        {
            Inventory = new InventoryHandPump(null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(ServerTick, 200);
                return;
            }
            if (api is ICoreClientAPI capi)
            {
                pumpSound = capi.World.LoadSound(new SoundParams()
                {
                    Location = pumpSfx,
                    ShouldLoop = false,
                    DisposeOnFinish = false,
                    RelativePosition = false,
                    Range = 12,
                    Position = Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f)
                });
                containerRenderer = new HandPumpContainerRenderer(capi, this);
                capi.Event.RegisterRenderer(containerRenderer, EnumRenderStage.Opaque, "hod-handpump-container");
                Inventory.SlotModified += _ => containerRenderer?.ScheduleMeshUpdate();
            }
        }

        public bool TryStartPumping(IPlayer player)
        {
            if (PumpingPlayer != null) return false;
            if (ContainerSlot.Empty) return false;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerTopOpened) return false;

            PumpingPlayer = player;

            strokeTimer = 0f;
            strokeInProgress = false;
            lastSecondsUsed = 0f;
            var spring = FindWellViaNetwork(); 
            remainingPrimingStrokes = spring != null ? ComputePrimingStrokes(Api.World, Pos, spring.Pos) : 0;
            MarkDirty();
            return true;
        }

        public void StopPumping()
        {
            PumpingPlayer = null;
            strokeInProgress = false;

            pumping = false;
            MarkDirty();
            if (Api.Side == EnumAppSide.Server)
            {
                var pkt = new PumpSfxPacket { X = Pos.X, Y = Pos.Y, Z = Pos.Z, Start = false };
                ((ICoreServerAPI)Api).Network
                    .GetChannel(HydrateOrDiedrateModSystem.NetworkChannelID)
                    .BroadcastPacket(pkt);
            }
        }


        public bool ContinuePumping(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return true;
            if (PumpingPlayer == null || ContainerSlot.Empty) return false;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerTopOpened cont) return false;
            
            var spring = FindWellViaNetwork();
            if (spring == null) return true;

            if (!strokeInProgress)
            {
                StartStroke();
                strokeTimer = 0f;
            }

            if (strokeInProgress)
            {
                float phase = strokeTimer / StrokePeriodSec;

                if (phase >= 0.6f && willEmitParticlesThisStroke)
                {
                    particleAcc += 40f * Math.Max(0f, dt);
                    int emit = (int)particleAcc;
                    if (emit > 0)
                    {
                        particleAcc -= emit;

                        var stack = BuildWellFillStack(spring);
                        if (stack != null)
                        {
                            GetSpout(out var spoutPos, out var spoutDir);
                            byte[] stackBytes;
                            using (var ms = new MemoryStream())
                            using (var bw = new BinaryWriter(ms))
                            {
                                stack.ToBytes(bw);
                                stackBytes = ms.ToArray();
                            }

                            var pkt = new PumpParticleBurstPacket
                            {
                                PosX = spoutPos.X, PosY = spoutPos.Y, PosZ = spoutPos.Z,
                                DirX = spoutDir.X, DirY = 0f, DirZ = spoutDir.Z,

                                Quantity = Math.Min(emit, 14),
                                Radius = 0.08f,
                                Scale = 0.23f,
                                Speed = 0.3f,
                                VelJitter = 0.0f,
                                LifeMin = 0.1f,
                                LifeMax = 0.2f,
                                Gravity = 1.2f,

                                StackBytes = stackBytes
                            };

                            ((ICoreServerAPI)Api).Network
                                .GetChannel(HydrateOrDiedrateModSystem.NetworkChannelID)
                                .BroadcastPacket(pkt);
                        }
                    }
                }
            }

            strokeTimer += Math.Max(0f, dt);
            if (strokeTimer >= StrokePeriodSec)
            {
                strokeTimer -= StrokePeriodSec;
                CompleteStroke(cont, spring);
                StartStroke();
            }

            return true;
        }

        private void StartStroke()
        {
            strokeInProgress = true;
            if (Api.Side == EnumAppSide.Server)
            {
                var pkt = new PumpSfxPacket { X = Pos.X, Y = Pos.Y, Z = Pos.Z, Start = true };
                ((ICoreServerAPI)Api).Network
                    .GetChannel(HydrateOrDiedrateModSystem.NetworkChannelID)
                    .BroadcastPacket(pkt);
            }

            willEmitParticlesThisStroke = false;
            willExtractLitresThisStroke = 0;

            if (remainingPrimingStrokes <= 0)
            {
                var spring = FindWellViaNetwork();
                if (spring != null)
                {
                    int wholeLitresThisStroke = Math.Max(0, (int)Math.Floor(LitresPerProductiveStroke));
                    if (wholeLitresThisStroke > 0)
                    {
                        var cont = ContainerSlot.Itemstack?.Collectible as BlockLiquidContainerBase;
                        if (cont != null)
                        {
                            float curL = cont.GetCurrentLitres(ContainerSlot.Itemstack);
                            int freeLitres = (int)Math.Floor(Math.Max(0f, cont.CapacityLitres - curL));

                            var existing = cont.GetContent(ContainerSlot.Itemstack);
                            var filter   = existing?.Collectible?.Code;

                            var fillStack = BuildWellFillStack(spring);
                            bool canMatchFilter = filter == null || (fillStack != null && fillStack.Collectible?.Code == filter);
                            willEmitParticlesThisStroke = (fillStack != null);
                            bool canExtract = fillStack != null && canMatchFilter && freeLitres > 0;
                            willExtractLitresThisStroke = canExtract ? wholeLitresThisStroke : 0;
                        }
                    }
                }
            }

            if (Api.Side == EnumAppSide.Server)
            {
                pumping = true;
                MarkDirty();
            }
        }

        private void CompleteStroke(BlockLiquidContainerBase cont, BlockEntityWellSpring spring)
        {
            strokeInProgress = false;
            if (Api.Side == EnumAppSide.Server)
            {
                pumping = false;
                MarkDirty();

                var pkt = new PumpSfxPacket { X = Pos.X, Y = Pos.Y, Z = Pos.Z, Start = false };
                ((ICoreServerAPI)Api).Network
                    .GetChannel(HydrateOrDiedrateModSystem.NetworkChannelID)
                    .BroadcastPacket(pkt);
            }

            if (remainingPrimingStrokes > 0)
            {
                remainingPrimingStrokes--;
                return;
            }

            int wholeLitresThisStroke = Math.Max(0, (int)Math.Floor(LitresPerProductiveStroke));
            if (wholeLitresThisStroke <= 0) return;

            if (willExtractLitresThisStroke <= 0) return;

            float curL = cont.GetCurrentLitres(ContainerSlot.Itemstack);
            int freeLitres = (int)Math.Floor(Math.Max(0f, cont.CapacityLitres - curL));

            var existing = cont.GetContent(ContainerSlot.Itemstack);
            var filter   = existing?.Collectible?.Code;

            int requestTotal = Math.Min(willExtractLitresThisStroke, wholeLitresThisStroke);
            int toStore = 0;

            var fillStack = BuildWellFillStack(spring);
            bool filterOK = filter == null || (fillStack != null && fillStack.Collectible?.Code == filter);
            if (freeLitres > 0 && filterOK)
                toStore = Math.Min(requestTotal, freeLitres);

            int toWaste = requestTotal - toStore;

            if (toStore > 0)
            {
                var stackStored = ExtractStackFromWell(spring, toStore, filter, out int litresStored);
                if (stackStored != null && litresStored > 0)
                {
                    var itemProps = stackStored.Collectible.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
                    int movedItems = (itemProps != null)
                        ? (int)Math.Round(itemProps.ItemsPerLitre * litresStored)
                        : stackStored.StackSize;

                    if (existing != null) stackStored.StackSize += existing.StackSize;

                    cont.SetContent(ContainerSlot.Itemstack, stackStored);
                    ContainerSlot.MarkDirty();
                    MarkDirty();

                    if (litresStored < toStore)
                        toWaste += (toStore - litresStored);
                }
                else
                {
                    toWaste += toStore;
                }
            }

            if (toWaste > 0)
            {
                ExtractStackFromWell(spring, toWaste, filter: null, out int _);
            }
        }

        private void GetSpout(out Vec3d pos, out Vec3f dir)
        {
            var block = Api.World.BlockAccessor.GetBlock(Pos);
            string side = block?.Variant?["side"] ?? block?.Variant?["horizontalorientation"] ?? "north";
            var face = BlockFacing.FromCode(side) ?? BlockFacing.NORTH;

            var fwd = new Vec3f(face.Normali.X, 0f, face.Normali.Z);

            const double baseY = 0.43;
            const double upOffset = 0.08;
            const double forwardOffset = 0.2;

            pos = Pos.ToVec3d().Add(0.5, baseY + upOffset, 0.5);
            pos.Add(fwd.X * forwardOffset, 0.0, fwd.Z * forwardOffset);

            dir = fwd;
        }

        public static void PlayPumpParticleBurst(ICoreClientAPI capi, PumpParticleBurstPacket msg)
        {
            ItemStack stack = null;
            if (msg.StackBytes != null && msg.StackBytes.Length > 0)
            {
                using var ms = new MemoryStream(msg.StackBytes);
                using var br = new BinaryReader(ms);
                stack = new ItemStack();
                stack.FromBytes(br);
                stack.ResolveBlockOrItem(capi.World);
            }

            if (stack == null || stack.Collectible == null) return;

            var fwd = new Vec3f(msg.DirX, 0f, msg.DirZ);
            var right = new Vec3f(-fwd.Z, 0f, fwd.X);

            var vel = new Vec3f(
                msg.DirX * (msg.Speed + 0.2f),
                msg.DirY * (msg.Speed + 0.2f),
                msg.DirZ * (msg.Speed + 0.2f)
            );

            float lateral = msg.VelJitter > 0f ? msg.VelJitter : 0.02f;
            float axial = lateral * 0.3f;

            var p0 = new PumpCubeParticles(
                    collisionPos: new Vec3d(msg.PosX, msg.PosY, msg.PosZ),
                    stack: stack,
                    radius: msg.Radius,
                    quantity: msg.Quantity,
                    scale: msg.Scale,
                    velocity: vel
                )
                .UseDirectional(lateral, axial)
                .SetLifeRange(0.05f, 0.1f)
                .SetGravity(0f);

            capi.World.SpawnParticles(p0);

            const int gravityDelayMs = 150;
            const double rightNudge = 0.0;
            const double fwdNudge = 0.1;
            const double upNudge = 0.01;

            var p1pos = new Vec3d(
                msg.PosX + right.X * rightNudge + fwd.X * fwdNudge,
                msg.PosY + upNudge,
                msg.PosZ + right.Z * rightNudge + fwd.Z * fwdNudge
            );

            capi.Event.RegisterCallback(dt =>
            {
                var p1 = new PumpCubeParticles(
                        collisionPos: p1pos,
                        stack: stack,
                        radius: msg.Radius,
                        quantity: msg.Quantity,
                        scale: msg.Scale,
                        velocity: vel
                    )
                    .UseDirectional(lateral, axial)
                    .SetLifeRange(msg.LifeMin, msg.LifeMax)
                    .SetGravity(msg.Gravity);

                capi.World.SpawnParticles(p1);
            }, gravityDelayMs);
        }
        
        public static void OnClientPumpSfx(ICoreClientAPI capi, PumpSfxPacket msg)
        {
            var pos = new BlockPos(msg.X, msg.Y, msg.Z);
            var be = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityHandPump;
            be?.HandlePumpSfxClient(msg);
        }
        
        private void EnsurePumpSound()
        {
            if (pumpSound != null) return;
            if (Api is not ICoreClientAPI capi) return;

            pumpSound = capi.World.LoadSound(new SoundParams
            {
                Location = pumpSfx,
                ShouldLoop = false,
                DisposeOnFinish = false,
                RelativePosition = false,
                Range = 12,
                Position = Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f)
            });
        }

        public void HandlePumpSfxClient(PumpSfxPacket pkt)
        {
            if (Api.Side != EnumAppSide.Client) return;
            EnsurePumpSound();
            if (pumpSound == null) return;

            pumpSound.SetPosition(Pos.ToVec3f().Add(0.5f, 0.5f, 0.5f));

            if (pkt.Start)
            {
                if (pumpSound.IsPlaying) pumpSound.Stop();
                pumpSound.Start();
            }
            else
            {
                if (pumpSound.IsPlaying) pumpSound.Stop();
            }
        }
        
        private int ComputePrimingStrokes(IWorldAccessor world, BlockPos start, BlockPos targetSpringPos)
        {
            var open = new Queue<(BlockPos pos, int pipeCount)>();
            var seen = new HashSet<BlockPos>(new HydrateOrDiedrate.Piping.FluidNetwork.FluidSearch.PosCmp());
            var first = start.DownCopy();
            open.Enqueue((first, 0));
            seen.Add(first);

            while (open.Count > 0)
            {
                var (cur, pipesSoFar) = open.Dequeue();
                if (SamePos(cur, targetSpringPos))
                {
                    return pipesSoFar / 3;
                }

                foreach (var face in BlockFacing.ALLFACES)
                {
                    var next = cur.AddCopy(face);
                    if (!seen.Add(next)) continue;
                    if (SamePos(next, targetSpringPos))
                    {
                        return pipesSoFar / 5;
                    }

                    var nb = world.BlockAccessor.GetBlock(next);
                    if (nb is not IFluidBlock nFluid) continue;
                    var curBlock = world.BlockAccessor.GetBlock(cur) as IFluidBlock;
                    bool selfAllows     = curBlock?.HasFluidConnectorAt(world, cur, face) ?? true;
                    bool neighborAllows = nFluid.HasFluidConnectorAt(world, next, face.Opposite);
                    if (!selfAllows || !neighborAllows) continue;

                    int nextPipes = pipesSoFar + (nb is HydrateOrDiedrate.Piping.Pipe.BlockPipe ? 1 : 0);
                    open.Enqueue((next, nextPipes));
                }
            }

            return 0; 
        }
        
        private BlockEntityWellSpring FindWellViaNetwork()
        {
            return FluidSearch.TryFindWellSpring(Api.World, Pos, out var found, maxVisited: 4096)
                ? found
                : null;
        }
        
        private void ServerTick(float dt)
        {
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);

            if (tree.HasAttribute("inventory"))
            {
                var inv = tree.GetTreeAttribute("inventory");
                Inventory.FromTreeAttributes(inv);
                if (Api != null) Inventory.AfterBlocksLoaded(Api.World);
            }

            pendingDrawLitres = tree.GetFloat("pendingDrawLitres", pendingDrawLitres);
            pumping = tree.GetBool("pumping", pumping);

            if (Api?.Side == EnumAppSide.Client)
            {
                if (AnimBh != null)
                {
                    if (pumping && !AnimBh.IsPumping) AnimBh.StartPumpAnim();
                    if (!pumping && AnimBh.IsPumping) AnimBh.StopPumpAnim();
                }
                containerRenderer?.ScheduleMeshUpdate();
            }
        }
        
        private ItemStack BuildWellFillStack(BlockEntityWellSpring spring)
        {
            var fluidBlock = GetRepresentativeWellBlockForFilling(spring);
            if (fluidBlock?.Attributes == null) return null;

            var props = fluidBlock.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            var stack = props?.WhenFilled?.Stack;
            if (stack == null) return null;
            if (!stack.Resolve(Api.World, nameof(BlockEntityHandPump))) return null;

            return stack.ResolvedItemstack;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            var invTree = new TreeAttribute();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            tree.SetFloat("pendingDrawLitres", pendingDrawLitres);
            tree.SetBool("pumping", pumping);
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Server)
            {
                var pkt = new PumpSfxPacket { X = Pos.X, Y = Pos.Y, Z = Pos.Z, Start = false };
                ((ICoreServerAPI)Api).Network
                    .GetChannel(HydrateOrDiedrateModSystem.NetworkChannelID)
                    .BroadcastPacket(pkt);

                Inventory?.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            base.OnBlockRemoved();
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel) => false;
        
        private ItemStack ExtractStackFromWell(BlockEntityWellSpring spring, int litresToExtract, AssetLocation filter, out int litresExtracted)
        {
            litresExtracted = 0;
            if (spring == null || litresToExtract <= 0) return null;

            var fluidBlock = GetRepresentativeWellBlockForFilling(spring);
            if (fluidBlock?.Attributes == null) return null;

            var props = fluidBlock.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            var stack = props?.WhenFilled?.Stack;
            if (stack == null || !stack.Resolve(Api.World, nameof(BlockEntityHandPump))) return null;
            if (filter != null && stack.Code != filter) return null;

            int delta = spring.TryChangeVolume(-litresToExtract);
            litresExtracted = -delta;
            if (litresExtracted <= 0) return null;

            var itemProps = stack.ResolvedItemstack.Collectible.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            if (itemProps == null) return null;

            stack.ResolvedItemstack.StackSize = (int)Math.Round(itemProps.ItemsPerLitre * litresExtracted);
            return stack.ResolvedItemstack;
        }
        private Block GetRepresentativeWellBlockForFilling(BlockEntityWellSpring spring)
        {
            string baseCode = $"wellwater-{(spring.IsFresh ? "fresh" : "salt")}-{spring.currentPollution}";
            var candidates = new[]
            {
                $"{baseCode}-natural-still-7",
                $"{baseCode}-natural-still-1",
                $"{baseCode}-natural-flowing-7",
                $"{baseCode}-natural-flowing-1"
            };

            foreach (var path in candidates)
            {
                var blk = Api.World.GetBlock(new AssetLocation("hydrateordiedrate", path));
                if (blk?.Attributes?["waterTightContainerProps"].Exists == true) return blk;
            }
            return null;
        }
        public override void Dispose()
        {
            base.Dispose();
            if (Api is ICoreClientAPI capi && containerRenderer != null)
            {
                if (Api is ICoreClientAPI && pumpSound != null)
                {
                    pumpSound.Stop();
                    pumpSound.Dispose();
                    pumpSound = null;
                }
                capi.Event.UnregisterRenderer(containerRenderer, EnumRenderStage.Opaque);
                containerRenderer.Dispose();
                containerRenderer = null;
            }
        }
    }
}
