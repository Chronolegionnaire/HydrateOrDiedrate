using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using static Vintagestory.GameContent.BlockLiquidContainerBase;

namespace HydrateOrDiedrate.Keg;

/// <summary>
/// Behavior for attaching liquid containers to a boat.
/// BEWARE: this currently only works with the following workarounds: liquid container needs to override OnHeldInteractStart (to prevent standard liquid interaction) and yangcoifix is required for actually putting stuff in and out of the container
/// </summary>
/// <param name="block"></param>
public class AttachableLiquidContainer(Block block) : BlockBehavior(block),  IAttachableToEntity, IAttachedInteractions
{
    public int RequiresBehindSlots { get; set; }

    public bool PreventDetachWithLiquid { get; set; } = true;

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        if(properties is null) return;
        PreventDetachWithLiquid = properties[nameof(PreventDetachWithLiquid)].AsBool(PreventDetachWithLiquid);
    }

    public void CollectTextures(ItemStack stack, Shape shape, string texturePrefixCode, Dictionary<string, CompositeTexture> intoDict)
    {
    }

    public CompositeShape GetAttachedShape(ItemStack stack, string slotCode) => block.Shape;

    public string GetCategoryCode(ItemStack stack)  => block.Attributes?["attachableCategoryCode"].AsString("chest");

    public string[] GetDisableElements(ItemStack stack) => null;

    public string[] GetKeepElements(ItemStack stack) => null;

    public string GetTexturePrefixCode(ItemStack stack) => null;

    public bool IsAttachable(Entity toEntity, ItemStack itemStack) => true;

    public void OnAttached(ItemSlot itemslot, int slotIndex, Entity toEntity, EntityAgent byEntity)
    {

    }

    public void OnDetached(ItemSlot itemslot, int slotIndex, Entity fromEntity, EntityAgent byEntity)
    {

    }

    public void OnEntityDeath(ItemSlot itemslot, int slotIndex, Entity onEntity, DamageSource damageSourceForDeath)
    {
    }

    public void OnEntityDespawn(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityDespawnData despawn)
    {

    }

    public void OnInteract(ItemSlot itemslot, int slotIndex, Entity onEntity, EntityAgent byEntity, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled, Action onRequireSave)
    {
        if(byEntity is not EntityPlayer playerEntity) return;
        if(itemslot.Itemstack?.Collectible is not BlockLiquidContainerBase container) return;
        if(byEntity.ActiveHandItemSlot.Itemstack is not { StackSize: > 0 } stack || stack.Collectible.GetCollectibleInterface<ILiquidInterface>() is not { } liquidInterface || !liquidInterface.AllowHeldLiquidTransfer) return;
        bool singleTake = byEntity.Controls.ShiftKey;
		bool singlePut = byEntity.Controls.CtrlKey;

        if (!singleTake && stack.Collectible.GetCollectibleInterface<ILiquidSource>() is { } liquidSource)
        {
            var contentStackToMove = liquidSource.GetContent(byEntity.ActiveHandItemSlot.Itemstack);

            float litres = singlePut ? liquidSource.TransferSizeLitres : liquidSource.CapacityLitres;

            int moved = container.TryPutLiquid(itemslot.Itemstack, contentStackToMove, litres);

            if (moved > 0)
            {
                container.SplitStackAndPerformAction(byEntity, byEntity.ActiveHandItemSlot, (stack) =>
                {
                    liquidSource.TryTakeContent(stack, moved);
                    return moved;
                });
                container.DoLiquidMovedEffects(playerEntity.Player, contentStackToMove, moved, EnumLiquidDirection.Pour);
                onRequireSave();
                return;
            }
        }

        if (!singlePut && stack.Collectible.GetCollectibleInterface<ILiquidSink>() is { } liquidSink && container.Code.Path != "keg-untapped")
        {
            ItemStack owncontentStack = container.GetContent(itemslot.Itemstack);

            if (owncontentStack is null) return;

            var liquidStackForParticles = owncontentStack.Clone();

            float litres = singleTake ? liquidSink.TransferSizeLitres : liquidSink.CapacityLitres;

            int moved = container.SplitStackAndPerformAction(byEntity, byEntity.ActiveHandItemSlot, (stack) => liquidSink.TryPutLiquid(stack, owncontentStack, litres));
            if (moved > 0)
            {
                container.TryTakeContent(itemslot.Itemstack, moved);
                container.DoLiquidMovedEffects(playerEntity.Player, liquidStackForParticles, moved, EnumLiquidDirection.Fill);
                onRequireSave();
                return;
            }
        }

    }

    public void OnReceivedClientPacket(ItemSlot itemslot, int slotIndex, Entity onEntity, IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled, Action onRequireSave)
    {

    }

    public bool OnTryAttach(ItemSlot itemslot, int slotIndex, Entity toEntity)
    {
        return true;
    }

    public bool OnTryDetach(ItemSlot itemslot, int slotIndex, Entity toEntity)
    {
        if(PreventDetachWithLiquid && itemslot.Itemstack?.Collectible is BlockLiquidContainerBase liquidContainer && liquidContainer.GetCurrentLitres(itemslot.Itemstack) > 0)
        {
            if(toEntity.Api is ICoreClientAPI capi)
            {
                capi.TriggerIngameError(this, "hydrateordiedrate:container-detach-contains-liquid", Lang.Get("hydrateordiedrate:container-detach-contains-liquid"));
            }
            return false;
        }
        return true;
    }
}
