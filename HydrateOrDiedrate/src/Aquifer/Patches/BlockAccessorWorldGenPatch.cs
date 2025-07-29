using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using Vintagestory.API.MathTools;
using Vintagestory.Server;
using HydrateOrDiedrate;

[HarmonyPatch(typeof(BlockAccessorWorldGen))]
public static class BlockAccessorWorldGenPatch
{
    const int ChunkBits = 5;
    static readonly ConcurrentDictionary<ChunkKey, AquiferManager.WaterCounts> pending
        = new();
    readonly struct ChunkKey : IEquatable<ChunkKey>
    {
        public readonly int X, Z;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChunkKey(int x, int z) { X = x; Z = z; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ChunkKey other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is ChunkKey other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(X, Z);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(BlockAccessorWorldGen.SetFluidBlock))]
    public static void Prefix(int blockId, BlockPos pos, BlockAccessorWorldGen __instance)
    {
        var mgr = AquiferManager.Instance;
        if (mgr == null) return;

        var key    = new ChunkKey(pos.X >> ChunkBits, pos.Z >> ChunkBits);
        var counts = pending.GetOrAdd(key, _ =>
        {
            var cg = __instance.GetChunkAtBlockPos(pos);
            return cg?.GetModdata<AquiferManager.WaterCounts>(AquiferManager.WaterCountsKey, null)
                   ?? new AquiferManager.WaterCounts();
        });

        switch (mgr.GetWaterKind(blockId))
        {
            case AquiferManager.WaterKind.Normal:
            case AquiferManager.WaterKind.Ice:
                Interlocked.Increment(ref counts.NormalWaterBlockCount);
                break;
            case AquiferManager.WaterKind.Salt:
                Interlocked.Increment(ref counts.SaltWaterBlockCount);
                break;
            case AquiferManager.WaterKind.Boiling:
                Interlocked.Increment(ref counts.BoilingWaterBlockCount);
                break;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(BlockAccessorWorldGen.RunScheduledBlockLightUpdates))]
    public static void FlushCounts(BlockAccessorWorldGen __instance, int chunkx, int chunkz)
    {
        var key = new ChunkKey(chunkx, chunkz);
        if (!pending.TryRemove(key, out var counts)) return;
        var mapChunk = __instance.GetMapChunk(chunkx, chunkz) as ServerMapChunk;
        mapChunk?.SetModdata(AquiferManager.WaterCountsKey, counts);
    }
}