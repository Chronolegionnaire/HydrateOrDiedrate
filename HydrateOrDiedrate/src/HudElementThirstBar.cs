using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.Gui
{
    public class HudElementThirstBar : HudElement
    {
        private GuiElementStatbar _statbar;

        public override double InputOrder => 1.0;

        public HudElementThirstBar(ICoreClientAPI capi) : base(capi)
        {
            ComposeGuis();
        }

        public void OnGameTick(float dt)
        {
            UpdateThirst();
        }

        public void OnFlashStatbar(float dt)
        {
            var thirstTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("thirst");

            if (thirstTree != null && _statbar != null)
            {
                _statbar.ShouldFlash = _statbar.GetValue() < 0.2f;
            }
        }

        private void UpdateThirst()
        {
            if (_statbar == null) return;

            var currentThirst = capi.World.Player.Entity.WatchedAttributes.GetFloat("currentThirst");
            var maxThirst = capi.World.Player.Entity.WatchedAttributes.GetFloat("maxThirst");

            var lineInterval = maxThirst * 0.07f;

            _statbar.SetLineInterval(lineInterval);
            _statbar.SetValues(currentThirst, 0.0f, maxThirst);
        }

        private void ComposeGuis()
        {
            const float statsBarParentWidth = 850f;
            const float statsBarWidth = statsBarParentWidth * 0.41f;

            double[] thirstBarColor = { 0, 0.4, 0.5, 0.5 };

            var statsBarBounds = new ElementBounds()
            {
                Alignment = EnumDialogArea.CenterBottom,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = statsBarParentWidth,
                fixedHeight = 100
            }.WithFixedAlignmentOffset(0.0, 5.0);

            var isRight = true;
            var alignment = isRight ? EnumDialogArea.RightTop : EnumDialogArea.LeftTop;
            var alignmentOffsetX = isRight ? -2.0 : 1.0;

            var thirstBarBounds = ElementStdBounds.Statbar(alignment, statsBarWidth)
                .WithFixedAlignmentOffset(alignmentOffsetX, -16)
                .WithFixedHeight(10);

            var thirstBarParentBounds = statsBarBounds.FlatCopy().FixedGrow(0.0, 20.0);

            var composer = capi.Gui.CreateCompo("thirststatbar", thirstBarParentBounds);

            _statbar = new GuiElementStatbar(composer.Api, thirstBarBounds, thirstBarColor, isRight, false);

            composer
                .BeginChildElements(statsBarBounds)
                .AddInteractiveElement(_statbar, "thirststatsbar")
                .EndChildElements()
                .Compose();

            Composers["thirstbar"] = composer;

            TryOpen();
        }

        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            UpdateThirst();
        }

        public override void OnRenderGUI(float deltaTime)
        {
            if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator) return;

            base.OnRenderGUI(deltaTime);
        }

        public override bool TryClose() => false;

        public override bool ShouldReceiveKeyboardEvents() => false;

        public override bool Focusable => false;
    }
}
