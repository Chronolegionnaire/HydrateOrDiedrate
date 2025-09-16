using HarmonyLib;
using HydrateOrDiedrate.Wells.Aquifer;
using HydrateOrDiedrate.Wells.Aquifer.ModData;
using HydrateOrDiedrate.Config;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.patches;

[HarmonyPatch]
public static class ProspectingPatches
{
    [HarmonyPatch(typeof(ItemProspectingPick), "GenProbeResults")]
    [HarmonyPrefix]
    // Making a copy to avoid the mutation that happens in the original method
    public static void CacheBlockPos(BlockPos pos, out BlockPos __state) => __state = pos.Copy();

    [HarmonyPatch(typeof(ItemProspectingPick), "GenProbeResults")]
    [HarmonyPostfix]
    public static void AppendAquiferReading(ref PropickReading __result, IWorldAccessor world, BlockPos __state)
    {
        if (world.Side != EnumAppSide.Server || !ModConfig.Instance.GroundWater.ShowAquiferProspectingDataOnMap) return;
        
        var aquiferData = AquiferManager.GetAquiferChunkData(world, __state, world.Logger);
        if (aquiferData == null || aquiferData.Data.AquiferRating < AquiferData.MinimumAquiferRatingForDetection) return;
        
        __result.OreReadings[AquiferData.OreReadingKey] = aquiferData.Data;
    }

    [HarmonyPatch(typeof(PropickReading), "ToHumanReadable")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InjectHumanReadableString(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions, generator);

        matcher.MatchEndForward(
            CodeMatch.Calls(AccessTools.Method(typeof(Dictionary<string, OreReading>), nameof(Dictionary<string, OreReading>.GetEnumerator)))
        );
        matcher.MatchEndForward(new CodeMatch(OpCodes.Br)); //Find loop iterator branch
        
        var loopIterationTargetLabel = (Label)matcher.Instruction.operand;
        matcher.DefineLabel(out var continueLabel);
        var loopIterationTarget = matcher.Instructions().Find(code => code.labels.Contains(loopIterationTargetLabel));
        
        loopIterationTarget.labels.Add(continueLabel); //Define a new label (so we can 'continue')

        matcher.MatchEndForward(
            CodeMatch.Calls(AccessTools.PropertyGetter(typeof(Dictionary<string, OreReading>.Enumerator), nameof(Dictionary<string, OreReading>.Enumerator.Current))),
            CodeMatch.StoresLocal()
        );
        
        var localIndex = matcher.Instruction.GetStoredLocalIndex();
        matcher.InsertAfter(
            CodeInstruction.LoadLocal(localIndex),
            CodeInstruction.LoadLocal(0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ProspectingPatches), nameof(TryInsertAquiferReading))),
            new CodeInstruction(OpCodes.Brtrue, continueLabel) //If we added aquifer reading, skip the rest of the loop body
        );

        return matcher.InstructionEnumeration();
    }

    public static bool TryInsertAquiferReading(KeyValuePair<string, OreReading> reading, List<KeyValuePair<double, string>> readouts)
    {
        if(reading.Key != AquiferData.OreReadingKey) return false;
        
        var data = reading.Value;
        readouts.Add(new KeyValuePair<double, string>(0, AquiferData.GetProPickDescription((int)data.PartsPerThousand, data.DepositCode == "salty")));

        return true;
    }

    [HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
    [HarmonyPostfix]
    public static void AppendHintToPrintProbeResults(IWorldAccessor world, IServerPlayer splr, BlockPos pos)
    {
        if (world.Api.Side != EnumAppSide.Server || ModConfig.Instance.GroundWater.AquiferDataOnProspectingNodeMode) return;

        var hint = AquiferManager.GetAquiferDirectionHint(world, pos);

        SendMessageToPlayer(world, splr, hint);
    }

    [HarmonyPatch(typeof(ItemProspectingPick), "ProbeBlockNodeMode")]
    [HarmonyPostfix]
    public static void AppendHintToNodeMode(IWorldAccessor world, Entity byEntity, BlockSelection blockSel)
    {
        if (world.Api.Side != EnumAppSide.Server || !ModConfig.Instance.GroundWater.AquiferDataOnProspectingNodeMode || blockSel?.Position == null) return;
        if (byEntity is not EntityPlayer entityPlayer || world.PlayerByUid(entityPlayer.PlayerUID) is not IServerPlayer serverPlayer) return;
        
        var hint = AquiferManager.GetAquiferDirectionHint(world, blockSel.Position);
        
        SendMessageToPlayer(world, serverPlayer, hint);
    }

    private static void SendMessageToPlayer(IWorldAccessor world, IServerPlayer splr, string message) => world.Api.Event.EnqueueMainThreadTask(
        () => splr.SendMessage(GlobalConstants.InfoLogChatGroup, message, EnumChatType.Notification, null),
        "SendAquiferMessage"
    );
}