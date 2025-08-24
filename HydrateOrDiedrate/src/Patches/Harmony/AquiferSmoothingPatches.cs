using HarmonyLib;
using HydrateOrDiedrate.Aquifer;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.Common;
using Vintagestory.Server;

namespace HydrateOrDiedrate.Patches.Harmony;

[HarmonyPatch]
public static class AquiferSmoothingPatches
{
    [HarmonyPatch("Vintagestory.Server.ServerSystemSupplyChunks", "updateNeighboursLoadedFlags")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> HookOnNeighborChunksLoaded(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);
        
        matcher.DeclareLocal(typeof(int), out var chunkXVar);
        matcher.DeclareLocal(typeof(int), out var chunkZVar);

        matcher.MatchEndForward(
            new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(WorldMap), nameof(WorldMap.GetMapChunk)))
        );

        matcher.MatchEndBackwards(
            new CodeMatch(OpCodes.Add)
        );
        matcher.InsertAfter(
            CodeInstruction.StoreLocal(chunkZVar.LocalIndex),
            CodeInstruction.LoadLocal(chunkZVar.LocalIndex)
        );
        matcher.Advance(-1);

        matcher.MatchEndBackwards(
            new CodeMatch(OpCodes.Add)
        );
        matcher.InsertAfter(
            CodeInstruction.StoreLocal(chunkXVar.LocalIndex),
            CodeInstruction.LoadLocal(chunkXVar.LocalIndex)
        );

        matcher.MatchEndForward(
           new CodeMatch(OpCodes.Ldloc_2),
           new CodeMatch(OpCodes.Brfalse_S)
        );
        
        var toFindLabel = (Label)matcher.Instruction.operand;
        matcher.MatchStartForward(
            new CodeMatch(instruction => instruction.labels.Contains(toFindLabel))
        );
        
        matcher.Insert(
            CodeInstruction.LoadLocal(2),
            CodeInstruction.LoadLocal(chunkXVar.LocalIndex),
            CodeInstruction.LoadLocal(chunkZVar.LocalIndex),
            new (OpCodes.Call, AccessTools.Method(typeof(AquiferSmoothingPatches), nameof(OnNeighborChunksLoaded)))
        );

        return matcher.InstructionEnumeration();
    }

    public static void OnNeighborChunksLoaded(ServerMapChunk chunk, int chunkX, int chunkZ)
    {
        if(!chunk.NeighboursLoaded.Horizontals) return;
        if(!chunk.GetModdata<bool>(AquiferManager.NeedsSmoothingModDataKey)) return;
        AquiferManager.TrySmoothChunk(chunkX, chunkZ);
    }
}
