using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.Server;

namespace HydrateOrDiedrate;

[HarmonyPatch(typeof(BlockAccessorWorldGen), nameof(BlockAccessorWorldGen.SetFluidBlock))]
public class BlockAccessorWorldGen_SetFluidBlock_Patch
{
    static void Postfix(int blockId, BlockPos pos, BlockAccessorWorldGen __instance)
    {
        Block block = __instance.GetBlock(blockId);
        if (block == null) return;

        string blockCode = block.Code.ToString();
        if (!(blockCode.StartsWith("game:water") || blockCode.StartsWith("game:saltwater") || blockCode.StartsWith("game:boilingwater")))
            return;

        IWorldChunk chunk = __instance.GetChunkAtBlockPos(pos) as IWorldChunk;
        if (chunk == null) return;
        EnsureModdataInitialized(chunk, "game:water");
        EnsureModdataInitialized(chunk, "game:saltwater");
        EnsureModdataInitialized(chunk, "game:boilingwater");
        if (blockCode.StartsWith("game:saltwater"))
        {
            int saltWaterCount = chunk.GetModdata<int>("game:saltwater", 0);
            chunk.SetModdata("game:saltwater", saltWaterCount + 1);
        }
        else if (blockCode.StartsWith("game:boilingwater"))
        {
            int boilingWaterCount = chunk.GetModdata<int>("game:boilingwater", 0);
            chunk.SetModdata("game:boilingwater", boilingWaterCount + 1);
        }
        else if (blockCode.StartsWith("game:water"))
        {
            int normalWaterCount = chunk.GetModdata<int>("game:water", 0);
            chunk.SetModdata("game:water", normalWaterCount + 1);
        }

        chunk.MarkModified();
    }

    private static void EnsureModdataInitialized(IWorldChunk chunk, string key)
    {
        if (chunk.GetModdata<int?>(key) == null)
        {
            chunk.SetModdata(key, 0);
        }
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
                EnsureModdataInitialized(chunk, "game:water");
                EnsureModdataInitialized(chunk, "game:saltwater");
                EnsureModdataInitialized(chunk, "game:boilingwater");

                chunk.MarkModified();
            }
        }

        private static void EnsureModdataInitialized(IWorldChunk chunk, string key)
        {
            if (chunk.GetModdata<int?>(key) == null)
            {
                chunk.SetModdata(key, 0);
            }
        }
    }
}