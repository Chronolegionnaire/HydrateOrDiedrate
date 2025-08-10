using HydrateOrDiedrate.Aquifer;
using HydrateOrDiedrate.Aquifer.ModData;
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
            var test = capi.ChatCommands
                .Create("aquiferview") //TODO permisions
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

        private void RunLoop()
        {
            while (isOn)
            {
                try
                {
                    Thread.Sleep(250);
                    IClientPlayer player = capi.World.Player;
                    if (player is null) continue;

                    BlockPos playerPos = player.Entity.Pos.AsBlockPos;
                    int blockRadius = 100;
                    List<BlockPos> highlightPositions = new List<BlockPos>();
                    List<int> highlightColors = new List<int>();

                    for (int x = playerPos.X - blockRadius; x <= playerPos.X + blockRadius; x++)
                    {
                        for (int y = Math.Max(1, playerPos.Y - 50); y < Math.Min(capi.World.BlockAccessor.MapSizeY, playerPos.Y + 6); y++)
                        {
                            for (int z = playerPos.Z - blockRadius; z <= playerPos.Z + blockRadius; z++)
                            {
                                if (!isOn) break;

                                BlockPos currentPos = new(x, y, z);
                                Block block = capi.World.BlockAccessor.GetBlock(currentPos);

                                if (block is null || block.BlockId == 0 || !IsSoilGravelSandRock(block)) continue;
                                
                                if (!IsTopExposed(currentPos)) continue;

                                var aquiferData = AquiferManager.GetAquiferChunkData(capi.World, currentPos, capi.World.Logger)?.Data;
                                if (aquiferData is null) continue;

                                int color = GetColorForRating(aquiferData.AquiferRating, aquiferData.IsSalty);
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
