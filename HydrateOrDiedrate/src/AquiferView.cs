using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate
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
            this.capi = api;
            capi.ChatCommands
                .Create("aquiferview")
                .WithDescription("Toggle aquifer view highlight on/off")
                .HandleWith(OnCommandToggle);
        }

        private TextCommandResult OnCommandToggle(TextCommandCallingArgs args)
        {
            if (!isOn)
            {
                isOn = true;
                if (workerThread == null || !workerThread.IsAlive)
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

        private void RunLoop()
        {
            while (isOn)
            {
                try
                {
                    Thread.Sleep(250);
                    IClientPlayer player = capi.World.Player;
                    if (player == null) continue;

                    BlockPos playerPos = player.Entity.Pos.AsBlockPos;
                    int blockRadius = 100;
                    List<BlockPos> highlightPositions = new List<BlockPos>();
                    List<int> highlightColors = new List<int>();

                    for (int x = playerPos.X - blockRadius; x <= playerPos.X + blockRadius; x++)
                    {
                        for (int y = Math.Max(1, playerPos.Y - 6);
                             y < Math.Min(capi.World.BlockAccessor.MapSizeY, playerPos.Y + 6);
                             y++)
                        {
                            for (int z = playerPos.Z - blockRadius; z <= playerPos.Z + blockRadius; z++)
                            {
                                if (!isOn) break;

                                BlockPos currentPos = new BlockPos(x, y, z);
                                Block block = capi.World.BlockAccessor.GetBlock(currentPos);
                                if (block == null || block.BlockId == 0 || !IsSoilGravelSandRock(block)) continue;
                                if (!IsTopExposed(currentPos)) continue;

                                Vec2i chunkPos = new Vec2i(x / GlobalConstants.ChunkSize,
                                    z / GlobalConstants.ChunkSize);
                                var aquiferData = GetAquiferData(chunkPos);

                                if (aquiferData == null) continue;

                                int color = GetColorForRating(aquiferData.AquiferRating);
                                if (color == 0) continue;

                                highlightPositions.Add(currentPos);
                                highlightColors.Add(color);
                            }
                        }
                    }

                    capi.Event.EnqueueMainThreadTask(() =>
                    {
                        if (!isOn) return;
                        capi.World.HighlightBlocks(capi.World.Player, highlightId, highlightPositions, highlightColors);
                    }, "AquiferViewHighlight");
                }
                catch (Exception e)
                {
                    capi.Logger.Error("AquiferView thread exception: " + e);
                }
            }

            capi.Event.EnqueueMainThreadTask(
                () => { capi.World.HighlightBlocks(capi.World.Player, highlightId, new List<BlockPos>()); },
                "AquiferViewHighlightCleanup");
        }

        private bool IsTopExposed(BlockPos pos)
        {
            Block blockAbove = capi.World.BlockAccessor.GetBlock(pos.X, pos.Y + 1, pos.Z);
            return blockAbove != null && blockAbove.BlockId == 0;
        }


        private bool IsSoilGravelSandRock(Block block)
        {
            string category = block.Code?.Domain + ":" + block.Code?.Path;
            return category != null && (
                category.Contains("soil") ||
                category.Contains("gravel") ||
                category.Contains("sand") ||
                category.Contains("forestfloor") ||
                category.Contains("stone") ||
                category.Contains("muddygravel") ||
                category.Contains("rock")
            );
        }


        private AquiferManager.AquiferData GetAquiferData(Vec2i chunkCoord)
        {
            return HydrateOrDiedrateModSystem.HydrateOrDiedrateGlobals.AquiferManager?.GetAquiferData(chunkCoord);
        }

        private int GetColorForRating(int rating)
        {
            if (rating <= 20) return ColorUtil.ToRgba(128, 255, 200, 100); // Light Blue
            else if (rating <= 40) return ColorUtil.ToRgba(128, 0, 200, 0); // Green
            else if (rating <= 60) return ColorUtil.ToRgba(128, 0, 255, 255); // Yellow
            else if (rating <= 80) return ColorUtil.ToRgba(128, 0, 140, 255); // Orange
            else if (rating <= 100) return ColorUtil.ToRgba(128, 0, 0, 255); // Red
            return 0;
        }
    }
}
