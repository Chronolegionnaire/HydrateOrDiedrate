using HudShelf;
using HydrateOrDiedrate.Config;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace HydrateOrDiedrate.HUD;

public class HudElementThirstBar : HudElement
{
    private GuiElementStatbar _statbar;
    private bool _isFlashing;
    private int _tickCounter;
    private float _lastCurrentThirst = -1f;
    private float _lastMaxThirst = -1f;
    private float _lastLineInterval = -1f;
    
    public bool ShowThirstBar = true;
    public override double InputOrder => 1.0;
    
    private EntityBehaviorThirst thirstBehavior;

    public HudElementThirstBar(ICoreClientAPI capi) : base(capi)
    {
        ComposeGuis();
        capi.Event.RegisterGameTickListener(OnGameTick, 1000);

        if (capi.ModLoader.IsModEnabled("hudshelf"))
        {
            RegisterToHudShelf();
        }
    }

    private void RegisterToHudShelf()
    {
        var shelf = capi.ModLoader.GetModSystem<HudShelfModSystem>();
        
        var handle = shelf.Api.Register(new HudRegistration
        {
            Id = "hydrateordiedrate:thirstbar",
            Element = this,
            GetBounds = () => (SingleComposer?.Bounds.OuterWidth ?? 0, SingleComposer?.Bounds.OuterHeight ?? 0),
            OnPositionChanged = pos => 
            {
                pos.ApplyTo(SingleComposer.Bounds);
                SingleComposer.ReCompose();
            },
            DisplayName = "Thirst Bar",

            DefaultAnchor = HudAnchor.BottomCenter,
            DefaultOffsetX = 312,
            DefaultOffsetY = -115
        });

        capi.Event.RegisterCallback(_ =>
        {
            handle.CurrentPosition.ApplyTo(SingleComposer.Bounds);
            SingleComposer.ReCompose();
        }, 1);
    }

    public void OnGameTick(float dt)
    {
        if (!ModConfig.Instance.Thirst.Enabled || capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator) return;

        UpdateThirst();
        
        _tickCounter++;
        if (_tickCounter >= 12)
        {
            CheckFlash();
            _tickCounter = 0;
        }
    }

    private void CheckFlash()
    {
        if (_statbar == null) return;

        if (_lastCurrentThirst < _lastMaxThirst * 0.2f)
        {
            _isFlashing = !_isFlashing;
            _statbar.ShouldFlash = _isFlashing;
        }
        else
        {
            _statbar.ShouldFlash = false;
            _isFlashing = false;
        }
    }

    private void UpdateThirst()
    {
        if(thirstBehavior is null)
        {
            thirstBehavior = capi.World.Player?.Entity?.GetBehavior<EntityBehaviorThirst>();
            if(thirstBehavior is null) return;
        }
        if (_statbar == null) return;

        var currentThirst = thirstBehavior.CurrentThirst;
        var maxThirst = thirstBehavior.MaxThirst;

        if (currentThirst != _lastCurrentThirst || maxThirst != _lastMaxThirst)
        {
            if (maxThirst != _lastMaxThirst)
            {
                var newLineInterval = Math.Max(100f, maxThirst / 100f);
                if (Math.Abs(newLineInterval - _lastLineInterval) > 0.001f)
                {
                    _statbar.SetLineInterval(newLineInterval);
                    _lastLineInterval = newLineInterval;
                }
            }

            _statbar.SetValues(currentThirst, 0.0f, maxThirst);
            _lastCurrentThirst = currentThirst;
            _lastMaxThirst = maxThirst;
        }
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
            fixedWidth = statsBarWidth,
            fixedHeight = 10
        };
    
        var thirstBarBounds = ElementStdBounds.Statbar(EnumDialogArea.RightTop, statsBarWidth)
            .WithFixedHeight(10);
    
        var thirstBarParentBounds = statsBarBounds.FlatCopy()
            .WithFixedAlignmentOffset(250, -95);
    
        var composer = capi.Gui.CreateCompo("thirststatbar", thirstBarParentBounds);
    
        _statbar = new GuiElementStatbar(composer.Api, thirstBarBounds, thirstBarColor, true, false);
    
        composer
            .BeginChildElements(statsBarBounds)
            .AddInteractiveElement(_statbar, "thirststatsbar")
            .EndChildElements()
            .Compose();
    
        SingleComposer = composer;
    
        TryOpen();
    }
    public override void OnOwnPlayerDataReceived()
    {
        ComposeGuis();
        UpdateThirst();
    }
    public override void OnRenderGUI(float deltaTime)
    {
        if (!ModConfig.Instance.Thirst.Enabled) return;
        if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator) return;

        base.OnRenderGUI(deltaTime);
    }
    public override bool TryClose() => false;
    
    public override bool ShouldReceiveKeyboardEvents() => false;
    
    public override bool Focusable => false;
}