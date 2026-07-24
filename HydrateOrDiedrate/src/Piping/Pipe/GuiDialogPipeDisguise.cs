using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.Pipe
{
    public class GuiDialogPipeDisguise(ICoreClientAPI capi, IInventory inv, BlockPos pos) : GuiDialogBlockEntity("pipe-disguise", pos, capi)
    {
        readonly IInventory inv = inv;
        readonly BlockPos pos = pos;

        void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(pos.X, pos.Y, pos.Z, p);
        }

        public override string ToggleKeyCombinationCode => null;

        public void Compose()
        {
            var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            var compo = capi.Gui.CreateCompo($"pipe-disguise-{pos}", dialogBounds)
                .AddShadedDialogBG(ElementBounds.Fill, true)
                .AddDialogTitleBar(Lang.Get("hydrateordiedrate:pipedisguise"), () => TryClose());

            var content = ElementBounds.Fixed(0, 40, 180, 130).WithFixedPadding(10);
            compo.BeginChildElements(content);

            var slotBounds = ElementBounds.Fixed(0, 0, 80, 80).WithFixedPadding(8);

            compo.AddItemSlotGrid(inv, SendInvPacket, 1, slotBounds, "disguise-slots");

            compo.EndChildElements();
            SingleComposer = compo.Compose();
        }
        // TODO Make wrench able to switch rotational variant of disguise

        public override bool TryOpen(bool withFocus)
        {
            if (IsOpened()) return true;

            capi.World.Player.InventoryManager.OpenInventory(inv);

            Compose();
            capi.Gui.RegisterDialog(this);
            return base.TryOpen(withFocus);
        }

        public override bool TryClose()
        {
            capi.World.Player.InventoryManager.CloseInventory(inv);
            return base.TryClose();
        }
    }
}