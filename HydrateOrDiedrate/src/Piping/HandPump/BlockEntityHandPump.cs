// HydrateOrDiedrate.FluidNetwork.HandPump/BlockEntityHandPump.cs
using System;
using System.Collections.Generic;
using HydrateOrDiedrate.Piping.FluidNetwork;
using HydrateOrDiedrate.Wells.WellWater;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.Piping.HandPump
{
    public class BlockEntityHandPump : BlockEntityOpenableContainer
    {
        public const float PumpRateLitresPerSec = 1.25f;
        public const float IntakeLitresPerSec   = 8f;

        public IPlayer PumpingPlayer { get; private set; }
        public override string InventoryClassName => "handpump";
        public override InventoryBase Inventory { get; }
        public ItemSlot ContainerSlot => Inventory[0];

        private float pendingDrawLitres;
        private float pendingIntakeLitres;
        private BlockEntityWellSpring cachedWell;
        private double nextCacheCheckTotalDays;
        public float lastSecondsUsed;
        private const float StrokePeriodSec = 2f;
        private const float LitresPerProductiveStroke = 2f;
        private bool strokeInProgress;
        private static bool SamePos(BlockPos a, BlockPos b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        private float strokeTimer;               
        private int remainingPrimingStrokes;      
        private readonly AssetLocation pumpSfx = new AssetLocation("hydrateordiedrate", "sounds/pump1.ogg");
        private static SimpleParticleProperties _pumpWaterParticles;
        private static SimpleParticleProperties _pumpWhiteParticles;
        private float particleAcc;
        public BlockEntityHandPump()
        {
            Inventory = new InventoryGeneric(1, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (_pumpWaterParticles == null)
            {
                _pumpWaterParticles = CreateParticleProps(
                    ColorUtil.WhiteArgb,
                    new Vec3f(-0.25f, 0.0f, -0.25f),
                    new Vec3f(0.25f, 0.6f, 0.25f),
                    "climateWaterTint"
                );
            }
            if (_pumpWhiteParticles == null)
            {
                _pumpWhiteParticles = CreateParticleProps(
                    ColorUtil.ColorFromRgba(255, 255, 255, 128),
                    new Vec3f(-0.1f, 0.0f, -0.1f),
                    new Vec3f(0.1f, 0.2f, 0.1f)
                );
            }
            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(ServerTick, 200);
            }
        }

        public void InvalidateCachedWell() => cachedWell = null;

        public bool TryStartPumping(IPlayer player)
        {
            if (PumpingPlayer != null) return false;
            if (ContainerSlot.Empty) return false;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerBase) return false;

            PumpingPlayer = player;

            strokeTimer = 0f;
            strokeInProgress = false;
            lastSecondsUsed = 0f;
            cachedWell = ResolveWellSpring();
            remainingPrimingStrokes = cachedWell != null ? ComputePrimingStrokes(Api.World, Pos, cachedWell.Pos) : 0;

            MarkDirty();
            return true;
        }

        
        public void StopPumping()
        {
            PumpingPlayer = null;
            strokeInProgress = false;
            MarkDirty();
        }

        public bool ContinuePumping(float dt)
        {
            if (Api.Side != EnumAppSide.Server) return true;
            if (PumpingPlayer == null || ContainerSlot.Empty) return false;
            if (ContainerSlot.Itemstack?.Collectible is not BlockLiquidContainerBase cont) return false;

            ResolveWellSpring();
            if (cachedWell == null) return true;
            if (!strokeInProgress)
            {
                StartStroke();
                strokeTimer = 0f;
            }
            if (strokeInProgress)
            {
                particleAcc += 40f * Math.Max(0f, dt);
                int emit = (int)particleAcc;
                if (emit > 0)
                {
                    particleAcc -= emit;

                    // Build a representative stack so color derives from the actual liquid
                    ItemStack tintStack = null;
                    var repBlk = GetRepresentativeWellBlockForFilling(cachedWell);
                    if (repBlk != null) tintStack = new ItemStack(repBlk, 1);

                    EmitSpoutParticles(tintStack, Math.Min(emit, 8));
                }
            }
            strokeTimer += Math.Max(0f, dt);
            if (strokeTimer >= StrokePeriodSec)
            {
                strokeTimer -= StrokePeriodSec;
                CompleteStroke(cont);
                StartStroke();
            }

            return true;
        }

        private void StartStroke()
        {
            strokeInProgress = true;
            Api.World.PlaySoundAt(pumpSfx, Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5);
        }

        private void CompleteStroke(BlockLiquidContainerBase cont)
        {
            strokeInProgress = false;
            if (remainingPrimingStrokes > 0)
            {
                remainingPrimingStrokes--;
                return;
            }
            int wholeLitresThisStroke = Math.Max(0, (int)Math.Floor(LitresPerProductiveStroke));
            if (wholeLitresThisStroke <= 0) return;
            float curL = cont.GetCurrentLitres(ContainerSlot.Itemstack);
            int freeLitres = (int)Math.Floor(Math.Max(0f, cont.CapacityLitres - curL));
            if (freeLitres <= 0) return;

            int request = Math.Min(wholeLitresThisStroke, freeLitres);

            var existing = cont.GetContent(ContainerSlot.Itemstack);
            var filter   = existing?.Collectible?.Code;

            var stack = ExtractStackFromWell(cachedWell, request, filter, out int litresExtracted);
            if (stack == null || litresExtracted <= 0) return;

            var itemProps = stack.Collectible.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            int movedItems = (itemProps != null)
                ? (int)Math.Round(itemProps.ItemsPerLitre * litresExtracted)
                : stack.StackSize;
            EmitSpoutParticles(stack, 3);
            if (existing != null) stack.StackSize += existing.StackSize;

            cont.SetContent(ContainerSlot.Itemstack, stack);
            ContainerSlot.MarkDirty();
            MarkDirty();
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
                    // one dry stroke per 3 pipes
                    return pipesSoFar / 3;
                }

                foreach (var face in BlockFacing.ALLFACES)
                {
                    var next = cur.AddCopy(face);
                    if (!seen.Add(next)) continue;

                    // If the neighbor IS the wellspring, we’re done.
                    if (SamePos(next, targetSpringPos))
                    {
                        return pipesSoFar / 5; // don't count the spring itself as a pipe
                    }

                    var nb = world.BlockAccessor.GetBlock(next);
                    if (nb is not IFluidBlock nFluid) continue;

                    // both sides must expose connectors on touching faces
                    var curBlock = world.BlockAccessor.GetBlock(cur) as IFluidBlock;
                    bool selfAllows     = curBlock?.HasFluidConnectorAt(world, cur, face) ?? true;
                    bool neighborAllows = nFluid.HasFluidConnectorAt(world, next, face.Opposite);
                    if (!selfAllows || !neighborAllows) continue;

                    int nextPipes = pipesSoFar + (nb is HydrateOrDiedrate.Piping.Pipe.BlockPipe ? 1 : 0);
                    open.Enqueue((next, nextPipes));
                }
            }

            return 0; // no path found -> treat as no priming
        }

        private BlockEntityWellSpring ResolveWellSpring()
        {
            // Revalidate cache every ~2 in-game hours to survive pipe edits
            if (cachedWell != null && Api.World.Calendar.TotalDays < nextCacheCheckTotalDays)
                return cachedWell;

            cachedWell = null;

            if (FluidSearch.TryFindWellSpring(Api.World, Pos, out var found))
            {
                cachedWell = found;
                nextCacheCheckTotalDays = Api.World.Calendar.TotalDays + (2.0 / Api.World.Calendar.HoursPerDay);
            }

            return cachedWell;
        }

        private void ServerTick(float dt)
        {
            // reserved
        }

        // --- SAVE/LOAD (unchanged) ---
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
        }
        
        private void GetSpout(out Vec3d pos, out Vec3f dir)
        {
            var block = Api.World.BlockAccessor.GetBlock(Pos);
            string side = block?.Variant?["side"] ?? block?.Variant?["horizontalorientation"];
            BlockFacing face = BlockFacing.FromCode(side ?? "north") ?? BlockFacing.NORTH;

            // world position of the nozzle
            pos = Pos.ToVec3d().Add(0.5, 1.0, 0.5);
            pos.Add(face.Normali.X * 0.35, 0.10, face.Normali.Z * 0.35);

            // normalized direction the spout points
            dir = new Vec3f(face.Normali.X, 0f, face.Normali.Z);
        }
        
        private int ResolveLiquidColorFromStack(ItemStack stack)
        {
            if (stack == null) return _pumpWaterParticles?.Color ?? ColorUtil.WhiteArgb;

            // Prefer item attributes
            var attrs = stack.Collectible?.Attributes?["waterTightContainerProps"];
            if (attrs != null && attrs.Exists)
            {
                // Try common keys modders use
                int color = attrs["particleColor"].AsInt(-1);
                if (color == -1) color = attrs["color"].AsInt(-1);
                if (color != -1) return color;
            }

            // If the liquid is a block, also check the block's props
            if (stack.Collectible is Block blk && blk.Attributes?["waterTightContainerProps"].Exists == true)
            {
                var battrs = blk.Attributes["waterTightContainerProps"];
                int color = battrs["particleColor"].AsInt(-1);
                if (color == -1) color = battrs["color"].AsInt(-1);
                if (color != -1) return color;
            }

            // Fallback to the template color (may have climate tint if you keep it)
            return _pumpWaterParticles?.Color ?? ColorUtil.WhiteArgb;
        }

        private void EmitSpoutParticles(ItemStack colorStack, int quantity)
        {
            if (quantity <= 0) return;

            GetSpout(out var pos, out var dir);

            var template = _pumpWaterParticles;
            var p = new SimpleParticleProperties(
                template.MinQuantity,
                template.AddQuantity,
                ResolveLiquidColorFromStack(colorStack),   // <<< only change: derive color from stack
                new Vec3d(pos.X+0.2, pos.Y-0.48, pos.Z),
                new Vec3d(pos.X, pos.Y, pos.Z),
                template.MinVelocity, 
                template.AddVelocity + 500,
                template.MinSize,
                template.MaxSize,
                template.LifeLength,
                template.GravityEffect,
                template.ParticleModel
            );

            // Keep your existing spawn behavior exactly as-is
            p.AddPos = new Vec3d(0.01, 0.005, 0.01);
            p.MinVelocity = new Vec3f(dir.X * 0.2f, 0.05f, dir.Z * 0.2f);
            p.AddVelocity = new Vec3f(0.05f, 0.05f, 0.05f);
            p.MinQuantity = 1;
            p.AddQuantity = 0;
            p.MinSize = 0.15f;
            p.MaxSize = 0.22f;
            p.LifeLength = 0.6f;

            // If you want the stack color to dominate, clear climate tinting
            p.ClimateColorMap = null;

            for (int i = 0; i < quantity; i++)
            {
                Api.World.SpawnParticles(p);
            }
        }

        
        private static SimpleParticleProperties CreateParticleProps(
            int color, Vec3f minVelocity, Vec3f addVelocity, string climateColorMap = null)
        {
            var p = new SimpleParticleProperties(
                1, 1, color,
                new Vec3d(), new Vec3d(),
                minVelocity, addVelocity,
                0.1f, 0.1f, 0.5f, 1f,
                EnumParticleModel.Cube
            );
            p.AddPos = new Vec3d(0.125 / 2, 2 / 16f, 0.125 / 2);
            p.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f);
            p.AddQuantity = 1;
            p.ShouldDieInLiquid = true;
            p.ShouldSwimOnLiquid = true;
            p.GravityEffect = 1.5f;
            p.LifeLength = 2f;
            if (climateColorMap != null) p.ClimateColorMap = climateColorMap;
            return p;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            var invTree = new TreeAttribute();
            Inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;
            tree.SetFloat("pendingDrawLitres", pendingDrawLitres);
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Server && Inventory != null)
                Inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            base.OnBlockRemoved();
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel) => false;

        // === NEW: Winch-compatible extraction path ===

        // ExtractStackFromWell unchanged except: early-exit if fluid block lacks props
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


        // Hardened representative fluid block resolution
        private Block GetRepresentativeWellBlockForFilling(BlockEntityWellSpring spring)
        {
            // baseCode is exactly how the wellspring emits blocks
            string baseCode = $"wellwater-{(spring.IsFresh ? "fresh" : "salt")}-{spring.currentPollution}";

            // try common variants in order until we find one with attributes
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

    }
}
