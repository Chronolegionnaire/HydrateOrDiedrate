using System;
using System.IO;
using System.Text;
using HydrateOrDiedrate.Config;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Piping.Networking;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class BlockEntityHandPump : BlockEntityOpenableContainer
    {
        public IPlayer PumpingPlayer { get; private set; }

        public override string InventoryClassName => "handpump";
        public override InventoryBase Inventory { get; } =
            new InventorySingleTopOpenedContainer(null, null);

        public ItemSlot ContainerSlot => Inventory[0];

        private float pendingDrawLitres;
        public float lastSecondsUsed;
        private const float StrokePeriodSec = 2f;
        private const float LitresPerProductiveStroke = 2f;
        private bool strokeInProgress;
        private float strokeTimer;
        private int remainingPrimingStrokes;
        private float primeLevel;
        private double primeLastHours;
        private const float PrimeHalfLifeHours = 12f;
        private double nextDecayCheckHours;
        private int willExtractLitresThisStroke;
        private ILoadedSound pumpSound;
        private static readonly AssetLocation pumpSfx = new AssetLocation("hydrateordiedrate", "sounds/pump1.ogg");
        private bool pumping;
        private HandPumpContainerRenderer containerRenderer;
        private BlockEntityWellSpring currentSpring;
        private int lastNetworkVersion = -1;

        private BEBehaviorHandPumpAnim AnimBh => this.GetBehavior<BEBehaviorHandPumpAnim>();

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is not ICoreClientAPI capi)
            {
                RegisterGameTickListener(ServerTick, 200);
                return;
            }

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

        private void RefreshPrimeDecay()
        {
            if (Api?.World?.Calendar == null) return;

            double nowHours = Api.World.Calendar.TotalHours;
            double elapsed = nowHours - primeLastHours;
            if (primeLastHours == 0)
            {
                primeLastHours = nowHours;
                return;
            }

            if (elapsed <= 0 || primeLevel <= 0f)
            {
                primeLastHours = nowHours;
                return;
            }

            double factor = Math.Pow(0.5, elapsed / Math.Max(0.001, PrimeHalfLifeHours));
            float newPrime = (float)(primeLevel * factor);
            if (newPrime < 0.001f) newPrime = 0f;

            if (Math.Abs(newPrime - primeLevel) > 0.0001f)
            {
                primeLevel = newPrime;
                MarkDirty();
            }

            primeLastHours = nowHours;
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

            var spring = GetOrFindSpring();
            RefreshPrimeDecay();

            int required = spring != null ? ComputePrimingStrokes(Api.World, Pos, spring.Pos) : 0;
            int effectivePrime = GetEffectivePrimeInt(required);
            remainingPrimingStrokes = Math.Max(0, required - effectivePrime);

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
            var spring = GetOrFindSpring();

            if (!strokeInProgress)
            {
                StartStroke();
                strokeTimer = 0f;
            }

            if (strokeInProgress)
            {
                float phase = strokeTimer / StrokePeriodSec;
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
            willExtractLitresThisStroke = 0;

            if (remainingPrimingStrokes <= 0)
            {
                var spring = GetOrFindSpring();
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
                            var filter = existing?.Collectible?.Code;

                            var fillStack = BuildWellFillStack(spring);
                            bool canMatchFilter = filter == null || (fillStack != null && fillStack.Collectible?.Code == filter);
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
                var springNow = spring ?? GetOrFindSpring();
                int requiredNow = springNow != null ? ComputePrimingStrokes(Api.World, Pos, springNow.Pos) : 0;
                primeLevel = Math.Min(requiredNow, primeLevel + 1f);

                if (Api?.World?.Calendar != null)
                {
                    primeLastHours = Api.World.Calendar.TotalHours;
                }

                MarkDirty();
                return;
            }

            int wholeLitresThisStroke = Math.Max(0, (int)Math.Floor(LitresPerProductiveStroke));
            if (wholeLitresThisStroke <= 0) return;
            if (spring == null) return;

            if (willExtractLitresThisStroke <= 0) return;

            float curL = cont.GetCurrentLitres(ContainerSlot.Itemstack);
            int freeLitres = (int)Math.Floor(Math.Max(0f, cont.CapacityLitres - curL));

            var existing = cont.GetContent(ContainerSlot.Itemstack);
            var filter = existing?.Collectible?.Code;

            int requestTotal = Math.Min(willExtractLitresThisStroke, wholeLitresThisStroke);
            int toStore = 0;

            var fillStack = BuildWellFillStack(spring);
            bool filterOK = filter == null || (fillStack != null && fillStack.Collectible?.Code == filter);
            if (freeLitres > 0 && filterOK)
                toStore = Math.Min(requestTotal, freeLitres);

            int toWaste = requestTotal - toStore;

            int litresStored = 0;
            int litresWasted = 0;

            if (toStore > 0)
            {
                var stackStored = ExtractStackFromWell(spring, toStore, filter, out litresStored);
                if (stackStored != null && litresStored > 0)
                {
                    var itemProps = stackStored.Collectible.Attributes?["waterTightContainerProps"]
                        .AsObject<WaterTightContainableProps>();
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
                    litresStored = 0;
                }
            }

            if (toWaste > 0)
            {
                ExtractStackFromWell(spring, toWaste, filter: null, out litresWasted);
            }

            int totalExtracted = litresStored + litresWasted;
            if (totalExtracted > 0)
            {
                var visStack = BuildWellFillStack(spring);
                if (visStack != null)
                {
                    GetSpout(out var spoutPos, out var spoutDir);

                    byte[] stackBytes;
                    using (var ms = new MemoryStream())
                    using (var bw = new BinaryWriter(ms))
                    {
                        visStack.ToBytes(bw);
                        stackBytes = ms.ToArray();
                    }
                    EmitParticleSprayOverWindow(stackBytes, spoutPos, spoutDir, totalExtracted);
                }
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

        private void EmitParticleSprayOverWindow(byte[] stackBytes, Vec3d spoutPos, Vec3f spoutDir, int totalLitres)
        {
            if (Api.Side != EnumAppSide.Server) return;
            if (totalLitres <= 0) return;
            int durationMs = (int)(StrokePeriodSec * 0.4f * 1000f);
            int bursts = GameMath.Clamp(durationMs / 80, 5, 10);
            int totalQty = GameMath.Clamp(totalLitres * 6, 4, 14 * bursts);
            int basePerBurst = Math.Max(1, totalQty / bursts);
            int remainder = Math.Max(0, totalQty - basePerBurst * bursts);

            void BroadcastBurst(int qty)
            {
                var pkt = new PumpParticleBurstPacket
                {
                    PosX = (float)spoutPos.X,
                    PosY = (float)spoutPos.Y,
                    PosZ = (float)spoutPos.Z,
                    DirX = spoutDir.X,
                    DirY = 0f,
                    DirZ = spoutDir.Z,

                    Quantity = GameMath.Clamp(qty, 1, 14),
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

            for (int i = 0; i < bursts; i++)
            {
                int delay = (int)Math.Round(i * (durationMs / (double)bursts));
                int qty = basePerBurst + (i < remainder ? 1 : 0);

                ((ICoreServerAPI)Api).Event.RegisterCallback(_ => BroadcastBurst(qty), delay);
            }
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

        public void HandlePumpSfxClient(PumpSfxPacket pkt)
        {
            if (pumpSound is null) return;

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
            if (world == null || start == null || targetSpringPos == null) return 0;

            var startBelow = start.DownCopy();

            int distance = PipeTraversal.Distance(
                world,
                startBelow,
                BlockFacing.DOWN,
                (w, pos) =>
                    pos.X == targetSpringPos.X &&
                    pos.Y == targetSpringPos.Y &&
                    pos.Z == targetSpringPos.Z,
                maxVisited: 4096);

            if (distance <= 0) return 0;
            if (!ModConfig.Instance.Pump.HandPumpEnablePriming) return 0;
            int blocksPerStroke = ModConfig.Instance.Pump.HandPumpPrimingBlocksPerStroke;
            if (blocksPerStroke <= 0)
            {
                blocksPerStroke = 3;
            }
            return Math.Max(0, distance / blocksPerStroke);
        }

        private BlockEntityWellSpring FindWellViaNetwork()
        {
            return FluidSearch.TryFindWellSpring(Api.World, Pos, out var found, maxVisited: 4096)
                ? found
                : null;
        }

        private BlockEntityWellSpring GetOrFindSpring()
        {
            if (Api?.World == null) return null;

            int curVersion = FluidNetworkState.NetworkVersion;

            // If the graph hasn't changed since we last looked, trust the cache
            if (currentSpring != null && lastNetworkVersion == curVersion)
            {
                var be = Api.World.BlockAccessor.GetBlockEntity(currentSpring.Pos) as BlockEntityWellSpring;
                if (be == currentSpring)
                {
                    return currentSpring;
                }
            }
            currentSpring = FindWellViaNetwork();
            lastNetworkVersion = curVersion;
            return currentSpring;
        }

        private void ServerTick(float dt)
        {
            if (Api?.World?.Calendar == null) return;
            double nowHrs = Api.World.Calendar.TotalHours;
            if (nowHrs >= nextDecayCheckHours)
            {
                float before = primeLevel;
                RefreshPrimeDecay();
                nextDecayCheckHours = nowHrs + (10.0 / 60.0);
                if (Math.Abs(before - primeLevel) > 0.0001f) MarkDirty();
            }
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
            primeLevel = tree.GetFloat("primeLevel", primeLevel);
            primeLastHours = tree.GetDouble("primeLastHours", primeLastHours);

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

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            var invTree = new TreeAttribute();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
            tree.SetFloat("pendingDrawLitres", pendingDrawLitres);
            tree.SetBool("pumping", pumping);
            tree.SetFloat("primeLevel", primeLevel);
            tree.SetDouble("primeLastHours", primeLastHours);
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

            var itemProps = stack.ResolvedItemstack.Collectible.Attributes?["waterTightContainerProps"]
                .AsObject<WaterTightContainableProps>();
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);

            var displayPriming = ModConfig.Instance.Pump.HandPumpEnablePriming && ModConfig.Instance.Pump.HandPumpOutputInfo;
            var displayWellOutput = ModConfig.Instance.GroundWater.WellOutputInfo;

            if (!displayWellOutput && !displayPriming) return;

            var foundSpring = GetOrFindSpring();
            dsc.AppendLine();
            if (foundSpring is null)
            {
                dsc.AppendLine(Lang.Get("hydrateordiedrate:pump.noSpring"));
                return;
            }

            dsc.AppendLine(Lang.Get("hydrateordiedrate:pump.springDetected"));
            if (displayPriming)
            {
                int required = ComputePrimingStrokes(Api.World, Pos, foundSpring.Pos);

                if (required > 0)
                {
                    int already = GetEffectivePrimeInt(required);
                    int remaining = Math.Max(0, required - already);

                    dsc.Append("  ");
                    if (remaining > 0)
                    {
                        dsc.AppendLine(Lang.Get("hydrateordiedrate:pump.primingRemaining", remaining));
                    }
                    else
                    {
                        dsc.AppendLine(Lang.Get("hydrateordiedrate:pump.primed"));
                    }
                }
            }
            if (displayWellOutput) foundSpring.GetWellOutputInfo(forPlayer, dsc, true);
        }

        private int GetEffectivePrimeInt(int required)
        {
            if (primeLevel <= 0f) return 0;
            const float epsilon = 0.0001f;
            int effective = (int)Math.Ceiling(primeLevel - epsilon);

            return GameMath.Clamp(effective, 0, required);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Api is ICoreClientAPI capi && containerRenderer != null)
            {
                if (pumpSound is not null)
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
