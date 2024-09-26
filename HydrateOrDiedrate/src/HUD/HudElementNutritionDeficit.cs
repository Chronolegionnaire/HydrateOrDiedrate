using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace HydrateOrDiedrate.HUD
{
    public class HudElementHungerReductionBar : HudElement
    {
        private GuiElementCustomStatbar _statbar;
        private bool isFlashing;

        public override double InputOrder => 1.05;

        public HudElementHungerReductionBar(ICoreClientAPI capi) : base(capi)
        {
            ComposeGuis();
            capi.Event.RegisterGameTickListener(OnGameTick, 100);
            capi.Event.RegisterGameTickListener(OnFlashStatbars, 2500);
        }

        public void OnGameTick(float dt)
        {
            UpdateHungerReduction();
        }

        private void OnFlashStatbars(float dt)
        {
            if (_statbar == null) return;

            ITreeAttribute hungerTree = capi.World.Player.Entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (hungerTree == null) return;
            var player = capi.World.Player.Entity as EntityPlayer;
            var thirstBehavior = player?.GetBehavior<EntityBehaviorThirst>();
            if (thirstBehavior == null)
            {
                _statbar.CustomShouldFlash = false;
                return;
            }

            float hungerReductionAmount = thirstBehavior.HungerReductionAmount;
            float currentSaturation = hungerTree.GetFloat("currentsaturation");

            float displayValue = Math.Min(hungerReductionAmount, currentSaturation);

            if (displayValue > currentSaturation * 0.2f)
            {
                isFlashing = !isFlashing;
                _statbar.CustomShouldFlash = isFlashing;
            }
            else
            {
                _statbar.CustomShouldFlash = false;
            }
        }

        private void UpdateHungerReduction()
        {
            if (_statbar == null) return;

            var player = capi.World.Player.Entity as EntityPlayer;
            if (player == null)
            {
                return;
            }

            ITreeAttribute hungerTree = player.WatchedAttributes.GetTreeAttribute("hunger");
            if (hungerTree == null)
            {
                return;
            }
            float hungerReductionAmount = player.WatchedAttributes.GetFloat("hungerReductionAmount", 0f);
            float currentSaturation = hungerTree.GetFloat("currentsaturation");
            float maxSaturation = hungerTree.GetFloat("maxsaturation");
            float displayValue = Math.Min(hungerReductionAmount, currentSaturation);
            _statbar.SetCustomValues(displayValue, 0.0f, maxSaturation);
            _statbar.SetCustomLineInterval(maxSaturation / 15f);
        }


        private void ComposeGuis()
        {
            const float statsBarParentWidth = 850f;
            const float statsBarWidth = statsBarParentWidth * 0.41f;

            double[] hungerReductionBarColor = { 1.0, 0.5, 0.0, 0.7 };

            var statsBarBounds = new ElementBounds()
            {
                Alignment = EnumDialogArea.CenterBottom,
                BothSizing = ElementSizing.Fixed,
                fixedWidth = statsBarParentWidth,
                fixedHeight = 100
            }.WithFixedAlignmentOffset(0.0, 5.0);

            var isRight = true;
            var alignmentOffsetX = isRight ? -2.0 : 1.0;

            var hungerReductionBarBounds = ElementStdBounds.Statbar(isRight ? EnumDialogArea.RightTop : EnumDialogArea.LeftTop, statsBarWidth)
                .WithFixedAlignmentOffset(alignmentOffsetX, 5)
                .WithFixedHeight(10);

            var hungerReductionBarParentBounds = statsBarBounds.FlatCopy().FixedGrow(0.0, 20.0);

            var composer = capi.Gui.CreateCompo("hungerReductionStatbar", hungerReductionBarParentBounds);

            _statbar = new GuiElementCustomStatbar(composer.Api, hungerReductionBarBounds, hungerReductionBarColor, isRight, false);

            composer
                .BeginChildElements(statsBarBounds)
                .AddInteractiveElement(_statbar, "hungerReductionStatbar")
                .EndChildElements()
                .Compose();

            Composers["hungerReductionBar"] = composer;

            TryOpen();
        }

        public override void OnOwnPlayerDataReceived()
        {
            ComposeGuis();
            UpdateHungerReduction();
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
