using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.Server;

namespace HydrateOrDiedrate
{
    public enum WaterType
    {
        None,
        GameWater,
        SaltWater,
        BoilingWater
    }

    [ProtoContract]
    public class WaterCounts
    {
        [ProtoMember(1)]
        public int GameWater;

        [ProtoMember(2)]
        public int SaltWater;

        [ProtoMember(3)]
        public int BoilingWater;
    }

    [HarmonyPatch(typeof(BlockAccessorWorldGen), nameof(BlockAccessorWorldGen.SetFluidBlock))]
    public class BlockAccessorWorldGen_SetFluidBlock_Patch
    {
        private static readonly ConcurrentDictionary<IWorldChunk, WaterCounts> ChunkWaterCounts = new();
        static WaterType GetWaterType(string blockCode)
        {
            if (blockCode.StartsWith("game:water"))
                return WaterType.GameWater;
            if (blockCode.StartsWith("game:saltwater"))
                return WaterType.SaltWater;
            if (blockCode.StartsWith("game:boilingwater"))
                return WaterType.BoilingWater;
            return WaterType.None;
        }

        static void Postfix(int blockId, BlockPos pos, BlockAccessorWorldGen __instance)
        {
            Block block = __instance.GetBlock(blockId);
            if (block == null) return;

            WaterType waterType = GetWaterType(block.Code.ToString());
            if (waterType == WaterType.None) return;

            IWorldChunk chunk = __instance.GetChunkAtBlockPos(pos) as IWorldChunk;
            if (chunk == null) return;
            ChunkWaterCounts.AddOrUpdate(
                chunk,
                key =>
                {
                    var wc = new WaterCounts();
                    switch (waterType)
                    {
                        case WaterType.GameWater:
                            wc.GameWater = 1;
                            break;
                        case WaterType.SaltWater:
                            wc.SaltWater = 1;
                            break;
                        case WaterType.BoilingWater:
                            wc.BoilingWater = 1;
                            break;
                    }
                    return wc;
                },
                (key, existing) =>
                {
                    switch (waterType)
                    {
                        case WaterType.GameWater:
                            Interlocked.Increment(ref existing.GameWater);
                            break;
                        case WaterType.SaltWater:
                            Interlocked.Increment(ref existing.SaltWater);
                            break;
                        case WaterType.BoilingWater:
                            Interlocked.Increment(ref existing.BoilingWater);
                            break;
                    }
                    return existing;
                });
        }

        [HarmonyPatch]
        public class ChunkServerThread_GetGeneratingChunk_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(ChunkServerThread),
                    "GetGeneratingChunk",
                    new[] { typeof(int), typeof(int), typeof(int) }
                );
            }

            static void Postfix(object __result)
            {
                if (__result is ServerChunk chunk)
                {
                    const string compositeKey = "hydrateordiedrate:watercounts";
                    bool modified = EnsureModdataInitialized(chunk, compositeKey);

                    if (ChunkWaterCounts.TryRemove(chunk, out var counts))
                    {
                        WaterCounts waterCounts = chunk.GetModdata<WaterCounts>(compositeKey) ?? new WaterCounts();
                        Interlocked.Add(ref waterCounts.GameWater, counts.GameWater);
                        Interlocked.Add(ref waterCounts.SaltWater, counts.SaltWater);
                        Interlocked.Add(ref waterCounts.BoilingWater, counts.BoilingWater);

                        chunk.SetModdata(compositeKey, waterCounts);
                        modified = true;
                    }

                    if (modified)
                    {
                        chunk.MarkModified();
                    }
                }
            }

            private static bool EnsureModdataInitialized(IWorldChunk chunk, string key)
            {
                if (chunk.GetModdata<WaterCounts>(key) == null)
                {
                    chunk.SetModdata(key, new WaterCounts());
                    return true;
                }
                return false;
            }
        }
    }
}
