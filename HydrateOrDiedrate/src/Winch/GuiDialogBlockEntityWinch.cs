using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.winch
{
	public class GuiDialogBlockEntityWinch : GuiDialogBlockEntity
	{
		protected override double FloatyDialogPosition
		{
			get
			{
				return 0.75;
			}
		}
		public GuiDialogBlockEntityWinch(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
			: base(DialogTitle, Inventory, BlockEntityPosition, capi)
		{
			if (base.IsDuplicate)
			{
				return;
			}
			capi.World.Player.InventoryManager.OpenInventory(Inventory);
			this.SetupDialog();
		}
		private void OnInventorySlotModified(int slotid)
		{
			this.capi.Event.EnqueueMainThreadTask(new Action(this.SetupDialog), "setupwinchdlg");
		}
		private void SetupDialog()
		{
			ItemSlot hoveredSlot = this.capi.World.Player.InventoryManager.CurrentHoveredSlot;
			if (hoveredSlot != null && hoveredSlot.Inventory == base.Inventory)
			{
				this.capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
			}
			else
			{
				hoveredSlot = null;
			}
			ElementBounds winchBounds = ElementBounds.Fixed(0.0, 0.0, 100.0, 100.0);
			ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterMiddle, 0.0, 15.0, 1, 1);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(new ElementBounds[] { winchBounds });
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
			base.ClearComposers();
			IGuiAPI gui = this.capi.Gui;
			string text = "blockentitywinch";
			BlockPos blockEntityPosition = base.BlockEntityPosition;
			base.SingleComposer = gui.CreateCompo(text + ((blockEntityPosition != null) ? blockEntityPosition.ToString() : null), dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar(this.DialogTitle, new Action(this.OnTitleBarClose), null, null)
				.BeginChildElements(bgBounds)
				.AddDynamicCustomDraw(winchBounds, new DrawDelegateWithBounds(this.OnBgDraw), "symbolDrawer")
				.AddItemSlotGrid(base.Inventory, new Action<object>(this.SendInvPacket), 1, new int[1], inputSlotBounds, "inputSlot")
				.EndChildElements()
				.Compose(true);
			this.lastRedrawMs = this.capi.ElapsedMilliseconds;
			if (hoveredSlot != null)
			{
				base.SingleComposer.OnMouseMove(new MouseEvent(this.capi.Input.MouseX, this.capi.Input.MouseY));
			}
		}
		public void Update()
		{
			if (!this.IsOpened())
			{
				return;
			}
			if (this.capi.ElapsedMilliseconds - this.lastRedrawMs > 500L)
			{
				if (base.SingleComposer != null)
				{
					base.SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
				}
				this.lastRedrawMs = this.capi.ElapsedMilliseconds;
			}
		}
		private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
		{

		}
		private void SendInvPacket(object p)
		{
			this.capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, base.BlockEntityPosition.Y, base.BlockEntityPosition.Z, p);
		}
		private void OnTitleBarClose()
		{
			this.TryClose();
		}
		public override void OnGuiOpened()
		{
			base.OnGuiOpened();
			base.Inventory.SlotModified += this.OnInventorySlotModified;
		}
		public override void OnGuiClosed()
		{
			base.Inventory.SlotModified -= this.OnInventorySlotModified;
			base.SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(this.capi);
			base.OnGuiClosed();
		}
		private long lastRedrawMs;
		private float inputTurnTime;
		private float maxTurnTime;
	}
}
