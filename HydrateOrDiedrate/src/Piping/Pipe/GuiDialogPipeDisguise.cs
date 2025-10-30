using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.Piping.Pipe
{
    public class GuiDialogPipeDisguise : GuiDialog
    {
        readonly IInventory inv;
        readonly BlockPos pos;

        public GuiDialogPipeDisguise(ICoreClientAPI capi, IInventory inv, BlockPos pos) : base(capi)
        {
            this.inv = inv;
            this.pos = pos;
        }
        
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
                .AddDialogTitleBar(Lang.Get("hydrateordiedrate:Pipe disguise"), () => TryClose());

            var content = ElementBounds.Fixed(0, 40, 180, 130).WithFixedPadding(10);
            compo.BeginChildElements(content);

            var slotBounds = ElementBounds.Fixed(0, 0, 80, 80).WithFixedPadding(8);

            compo.AddItemSlotGrid(inv, SendInvPacket, 1, slotBounds, "disguise-slots");

            compo.AddStaticText(
                Lang.Get("Place a block to hide the pipe"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(0, 90, 170, 20)
            );

            compo.EndChildElements();
            SingleComposer = compo.Compose();
        }

        public bool TryOpen()
        {
            if (IsOpened()) return true;

            capi.World.Player.InventoryManager.OpenInventory(inv);

            Compose();
            capi.Gui.RegisterDialog(this);
            return base.TryOpen();
        }

        public override bool TryClose()
        {
            capi.World.Player.InventoryManager.CloseInventory(inv);
            return base.TryClose();
        }
    }
}
