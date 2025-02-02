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
			ElementBounds winchBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 90.0);
			ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 30.0, 1, 1);
			ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153.0, 30.0, 1, 1);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			bgBounds.WithChildren(new ElementBounds[] { winchBounds });
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
			base.ClearComposers();
			IGuiAPI gui = this.capi.Gui;
			string text = "blockentitywinch";
			BlockPos blockEntityPosition = base.BlockEntityPosition;
			base.SingleComposer = gui.CreateCompo(text + ((blockEntityPosition != null) ? blockEntityPosition.ToString() : null), dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar(this.DialogTitle, new Action(this.OnTitleBarClose), null, null)
				.BeginChildElements(bgBounds)
				.AddDynamicCustomDraw(winchBounds, new DrawDelegateWithBounds(this.OnBgDraw), "symbolDrawer")
				.AddItemSlotGrid(base.Inventory, new Action<object>(this.SendInvPacket), 1, new int[1], inputSlotBounds, "inputSlot")
				.AddItemSlotGrid(base.Inventory, new Action<object>(this.SendInvPacket), 1, new int[] { 1 }, outputSlotBounds, "outputslot")
				.EndChildElements()
				.Compose(true);
			this.lastRedrawMs = this.capi.ElapsedMilliseconds;
			if (hoveredSlot != null)
			{
				base.SingleComposer.OnMouseMove(new MouseEvent(this.capi.Input.MouseX, this.capi.Input.MouseY));
			}
		}
		public void Update(float inputTurnTime, float maxTurnTime)
		{
			this.inputTurnTime = inputTurnTime;
			this.maxTurnTime = maxTurnTime;
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
			double top = 30.0;
			ctx.Save();
			Matrix i = ctx.Matrix;
			i.Translate(GuiElement.scaled(63.0), GuiElement.scaled(top + 2.0));
			i.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
			ctx.Matrix = i;
			this.capi.Gui.Icons.DrawArrowRight(ctx, 2.0, true, true);
			double dx = (double)(this.inputTurnTime / this.maxTurnTime);
			ctx.Rectangle(GuiElement.scaled(5.0), 0.0, GuiElement.scaled(125.0 * dx), GuiElement.scaled(100.0));
			ctx.Clip();
			LinearGradient gradient = new LinearGradient(0.0, 0.0, GuiElement.scaled(200.0), 0.0);
			gradient.AddColorStop(0.0, new Color(0.0, 0.4, 0.0, 1.0));
			gradient.AddColorStop(1.0, new Color(0.2, 0.6, 0.2, 1.0));
			ctx.SetSource(gradient);
			this.capi.Gui.Icons.DrawArrowRight(ctx, 0.0, false, false);
			gradient.Dispose();
			ctx.Restore();
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
			base.SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(this.capi);
			base.OnGuiClosed();
		}
		private long lastRedrawMs;
		private float inputTurnTime;
		private float maxTurnTime;
	}
}
