using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.winch
{
	public class GuiDialogBlockEntityWinch : GuiDialogBlockEntity
	{
        protected override double FloatyDialogPosition => 0.75;

        public GuiDialogBlockEntityWinch(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi) : base(DialogTitle, Inventory, BlockEntityPosition, capi)
		{
			if (IsDuplicate) return;

			capi.World.Player.InventoryManager.OpenInventory(Inventory);
			SetupDialog();
		}
		private void OnInventorySlotModified(int slotid)
		{
            //TODOD: doe we realy need to even setup the dialog again?
			capi.Event.EnqueueMainThreadTask(SetupDialog, "setupwinchdlg");
		}

		private void SetupDialog()
		{
			ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
			if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
			{
				capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
			}
			else
			{
				hoveredSlot = null;
			}

			ElementBounds winchBounds = ElementBounds.Fixed(0.0, 0.0, 100.0, 100.0);
			ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterMiddle, 0.0, 15.0, 1, 1);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren([winchBounds]);
			
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
			
            ClearComposers();
			IGuiAPI gui = capi.Gui;
			string text = "blockentitywinch";
			BlockPos blockEntityPosition = BlockEntityPosition;
            SingleComposer = gui.CreateCompo(text + ((blockEntityPosition != null) ? ToString() : null), dialogBounds)
                .AddShadedDialogBG(bgBounds, true, 5.0, 0.75f)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose, null, null)
				.BeginChildElements(bgBounds)
				.AddDynamicCustomDraw(winchBounds, OnBgDraw, "symbolDrawer")
				.AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[1], inputSlotBounds, "inputSlot")
				.EndChildElements()
				.Compose(true);

			lastRedrawMs = capi.ElapsedMilliseconds;
			if (hoveredSlot != null)
			{
				SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
			}
		}
		public void Update()
		{
			if (!IsOpened()) return;

			if (capi.ElapsedMilliseconds - lastRedrawMs > 500L)
			{
				SingleComposer?.GetCustomDraw("symbolDrawer").Redraw();
				lastRedrawMs = capi.ElapsedMilliseconds;
			}
		}
		
        private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
		{
            //TODO: is there a reason for this empty custom draw??
		}
        
        private void SendInvPacket(object p) => capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);

        private void OnTitleBarClose() => TryClose();
        
        public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			Inventory.SlotModified += OnInventorySlotModified;
		}
		
        public override void OnGuiClosed()
		{
			Inventory.SlotModified -= OnInventorySlotModified;
			SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
			base.OnGuiClosed();
		}
		
        private long lastRedrawMs;
	}
}
