using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace HydrateOrDiedrate.Wells.Aquifer
{
    public class AquiferView : ModSystem
    {
        private ICoreClientAPI capi;
        private bool isOn;
        private Thread workerThread;
        private static readonly int highlightId = 9988;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            capi.ChatCommands
                .Create("aquiferview")
                .RequiresPrivilege(Privilege.controlserver)
                .WithDescription("Toggle aquifer view highlight on/off")
                .HandleWith(OnCommandToggle);
        }

        private TextCommandResult OnCommandToggle(TextCommandCallingArgs args)
        {
            if (!isOn)
            {
                isOn = true;
                if (workerThread is null || !workerThread.IsAlive)
                {
                    workerThread = new Thread(RunLoop)
                    {
                        IsBackground = true,
                        Name = "AquiferViewWorker"
                    };
                    workerThread.Start();
                }

                return TextCommandResult.Success("Aquifer rating highlighting is ON.");
            }
            else
            {
                isOn = false;
                capi.World.HighlightBlocks(capi.World.Player, highlightId, new List<BlockPos>());

                return TextCommandResult.Success("Aquifer rating highlighting is OFF.");
            }
        }

        private void PopulateHighlightBlocks(List<BlockPos> highlightPositions, List<int> highlightColors, FastVec3i chunkPos)
        {
            var chunk = capi.World.BlockAccessor.GetChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
            if(chunk is null) return;
            var aquiferData = AquiferManager.GetAquiferChunkData(chunk, capi.World.Logger)?.Data;
            if (aquiferData is null|| aquiferData.AquiferRating == 0) return;

            var startPos = new BlockPos(chunkPos.X * GlobalConstants.ChunkSize, chunkPos.Y * GlobalConstants.ChunkSize, chunkPos.Z * GlobalConstants.ChunkSize);
            var endPos = startPos + GlobalConstants.ChunkSize - 1;
            var tempPos = new BlockPos(capi.World.Player.Entity.Pos.Dimension);

            capi.World.BlockAccessor.WalkBlocks(startPos, endPos, (block, x, y, z) =>
            {
                if (block is null || block.BlockId == 0 || block.Replaceable >= 6000 || !block.SideSolid.All || !block.SideOpaque.All) return;
                tempPos.Set(x, y + 1, z);

                Block blockAbove = capi.World.BlockAccessor.GetBlock(tempPos);
                if (blockAbove is not null && blockAbove.BlockId != 0 && blockAbove.Replaceable < 6000) return;

                int color = GetColorForRating(aquiferData.AquiferRating, aquiferData.IsSalty);
                if (color == 0) return;

                highlightPositions.Add(new BlockPos(x, y, z));
                highlightColors.Add(color);
            });
        }

        private void RunLoop()
        {
            List<BlockPos> highlightPositions = new List<BlockPos>();
            List<int> highlightColors = new List<int>();
            
            while (isOn)
            {
                try
                {
                    do
                    {
                        Thread.Sleep(100);
                    }
                    while(highlightPositions.Count > 0);

                    IClientPlayer player = capi.World.Player;
                    if (player is null) continue;

                    BlockPos playerPos = player.Entity.Pos.AsBlockPos;
                    FastVec3i chunkPos = new(playerPos.X / GlobalConstants.ChunkSize, playerPos.InternalY / GlobalConstants.ChunkSize, playerPos.Z / GlobalConstants.ChunkSize);

                    const int chunkRadius = 2;
                    const int verticalChunkRadius = 2;

                    foreach(var x in Enumerable.Range(-chunkRadius, chunkRadius * 2 + 1)!)
                    {
                        foreach(var z in Enumerable.Range(-chunkRadius, chunkRadius * 2 + 1))
                        {
                            foreach(var y in Enumerable.Range(-verticalChunkRadius, verticalChunkRadius * 2 + 1))
                            {
                                PopulateHighlightBlocks(highlightPositions, highlightColors, new FastVec3i(chunkPos.X - x, chunkPos.Y - y, chunkPos.Z - z ));
                                Thread.Yield();
                            }
                        }
                    }

                    capi.Event.EnqueueMainThreadTask(() =>
                    {
                        try
                        {
                            if (!isOn) return;
                            capi.World.HighlightBlocks(capi.World.Player, highlightId, highlightPositions, highlightColors); //If we spread it over multiple highLightId's we could probably have less of a performance hit whenever we call this
                        }
                        finally
                        {
                            highlightColors.Clear();
                            highlightPositions.Clear();
                        }
                    }, "AquiferViewHighlight");
                }
                catch (Exception e)
                {
                    capi.Logger.Error("AquiferView thread exception: " + e);
                }
            }

            capi.Event.EnqueueMainThreadTask(
                () => capi.World.HighlightBlocks(capi.World.Player, highlightId, new List<BlockPos>()),
                "AquiferViewHighlightCleanup");
        }

        private int GetColorForRating(int rating, bool isSalty)
        {
            float t = Math.Max(0, Math.Min(100, rating)) / 100f;
            if (isSalty)
            {
                int startColor = ColorUtil.ToRgba(128, 200, 100, 255);
                int endColor   = ColorUtil.ToRgba(128, 255, 0, 0);
                return LerpColor(startColor, endColor, t);
            }
            else
            {
                int startColor = ColorUtil.ToRgba(128, 255, 200, 100);
                int endColor   = ColorUtil.ToRgba(128, 0, 0, 255);
                return LerpColor(startColor, endColor, t);
            }
        }

        private int LerpColor(int colorA, int colorB, float t)
        {
            int a1 = (colorA >> 24) & 0xFF;
            int r1 = (colorA >> 16) & 0xFF;
            int g1 = (colorA >> 8) & 0xFF;
            int b1 = colorA & 0xFF;

            int a2 = (colorB >> 24) & 0xFF;
            int r2 = (colorB >> 16) & 0xFF;
            int g2 = (colorB >> 8) & 0xFF;
            int b2 = colorB & 0xFF;

            int a = (int)(a1 + (a2 - a1) * t);
            int r = (int)(r1 + (r2 - r1) * t);
            int g = (int)(g1 + (g2 - g1) * t);
            int b = (int)(b1 + (b2 - b1) * t);

            return (a << 24) | (r << 16) | (g << 8) | b;
        }
    }
}
