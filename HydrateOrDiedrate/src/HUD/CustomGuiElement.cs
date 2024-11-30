using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace HydrateOrDiedrate.HUD;

public class GuiElementCustomStatbar : GuiElementTextBase
{
    public bool CustomHideWhenFull { get; set; }

    public GuiElementCustomStatbar(ICoreClientAPI capi, ElementBounds bounds, double[] customColor, bool customRightToLeft, bool customHideable) 
        : base(capi, "", CairoFont.WhiteDetailText(), bounds)
    {
        this.customBarTexture = new LoadedTexture(capi);
        this.customFlashTexture = new LoadedTexture(capi);
        this.customValueTexture = new LoadedTexture(capi);
        this.customHideable = customHideable;
        this.customColor = customColor;
        this.customRightToLeft = customRightToLeft;
        this.customOnGetStatbarValue = (() => ((float)Math.Round((double)this.customValue, 1)).ToString() + " / " + ((int)this.customMaxValue).ToString());
    }

    public override void ComposeElements(Context ctx, ImageSurface surface)
    {
        this.Bounds.CalcWorldBounds();
        if (this.customValuesSet)
        {
            this.RecomposeCustomOverlays();
        }
    }

    private void RecomposeCustomOverlays()
    {
        TyronThreadPool.QueueTask(delegate()
        {
            this.ComposeCustomValueOverlay();
        });
        if (this.CustomShowValueOnHover)
        {
            this.api.Gui.TextTexture.GenOrUpdateTextTexture(this.customOnGetStatbarValue(), this.customValueFont, ref this.customValueTexture, new TextBackground
            {
                FillColor = GuiStyle.DialogStrongBgColor,
                Padding = 5,
                BorderWidth = 2.0
            });
        }
    }

    private void ComposeCustomValueOverlay()
    {
        this.Bounds.CalcWorldBounds();
        double customWidthRel = (double)this.customValue / (double)(this.customMaxValue - this.customMinValue);
        this.customValueWidth = (int)(customWidthRel * this.Bounds.OuterWidth) + 1;
        this.customValueHeight = (int)this.Bounds.OuterHeight + 1;
        ImageSurface surface = new ImageSurface(Format.Argb32, this.Bounds.OuterWidthInt + 1, this.customValueHeight);
        Context ctx = new Context(surface);
        if (customWidthRel > 0.01)
        {
            double customWidth = this.Bounds.OuterWidth * customWidthRel;
            double customX = this.customRightToLeft ? (this.Bounds.OuterWidth - customWidth) : 0.0;
            GuiElement.RoundRectangle(ctx, customX, 0.0, customWidth, this.Bounds.OuterHeight, 1.0);
            ctx.SetSourceRGB(this.customColor[0], this.customColor[1], this.customColor[2]);
            ctx.FillPreserve();
            ctx.SetSourceRGB(this.customColor[0] * 0.4, this.customColor[1] * 0.4, this.customColor[2] * 0.4);
            ctx.LineWidth = GuiElement.scaled(3.0);
            ctx.StrokePreserve();
            surface.BlurFull(3.0);
            customWidth = this.Bounds.InnerWidth * customWidthRel;
            customX = (this.customRightToLeft ? (this.Bounds.InnerWidth - customWidth) : 0.0);
            base.EmbossRoundRectangleElement(ctx, customX, 0.0, customWidth, this.Bounds.InnerHeight, false, 2, 1);
        }
        ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.5); 
        ctx.LineWidth = GuiElement.scaled(2.2);
        int customLines = (int)((this.customMaxValue - this.customMinValue) / this.customLineInterval);
        
        customLines = Math.Max(1, customLines);

        for (int i = 1; i <= customLines; i++)
        {
            ctx.NewPath();
            double customX2 = this.Bounds.InnerWidth * ((double)i * this.customLineInterval) / (this.customMaxValue - this.customMinValue);
            ctx.MoveTo(customX2, 0.0);
            ctx.LineTo(customX2, Math.Max(3.0, this.Bounds.InnerHeight - 1.0));
            ctx.ClosePath();
            ctx.Stroke();
        }
        
        this.api.Event.EnqueueMainThreadTask(delegate
        {
            this.generateTexture(surface, ref this.customBarTexture, true);
            ctx.Dispose();
            surface.Dispose();
        }, "recompstatbar");
    }

    public override void RenderInteractiveElements(float deltaTime)
    {
        double customX = this.Bounds.renderX;
        double customY = this.Bounds.renderY;
        if (this.customValue == this.customMaxValue && this.CustomHideWhenFull)
        {
            return;
        }
        if (this.customBarTexture.TextureId > 0)
        {
            this.api.Render.RenderTexture(this.customBarTexture.TextureId, customX, customY, (double)(this.Bounds.OuterWidthInt + 1), (double)this.customValueHeight, 50f, null);
        }
        if (this.CustomShowValueOnHover && this.IsMouseOverColoredPart())
        {
            double customTx = (double)(this.api.Input.MouseX + 16);
            double customTy = (double)(this.api.Input.MouseY + this.customValueTexture.Height - 4);
            this.api.Render.RenderTexture(this.customValueTexture.TextureId, customTx, customTy, (double)this.customValueTexture.Width, (double)this.customValueTexture.Height, 2000f, null);
        }
    }

    private bool IsMouseOverColoredPart()
    {
        double customWidthRel = (double)this.customValue / (double)(this.customMaxValue - this.customMinValue);
        double coloredWidth = this.Bounds.InnerWidth * customWidthRel;
        double mouseX = this.api.Input.MouseX - this.Bounds.renderX;
        double mouseY = this.api.Input.MouseY - this.Bounds.renderY;
        if (this.customRightToLeft)
        {
            return mouseX >= (this.Bounds.InnerWidth - coloredWidth) && mouseX <= this.Bounds.InnerWidth && mouseY >= 0 && mouseY <= this.Bounds.InnerHeight;
        }
        else
        {
            return mouseX >= 0 && mouseX <= coloredWidth && mouseY >= 0 && mouseY <= this.Bounds.InnerHeight;
        }
    }


    public void SetCustomLineInterval(float value)
    {
        this.customLineInterval = value;
    }

    public void SetCustomValue(float value)
    {
        this.customValue = value;
        this.customValuesSet = true;
        this.RecomposeCustomOverlays();
    }

    public float GetCustomValue()
    {
        return this.customValue;
    }

    public void SetCustomValues(float value, float min, float max)
    {
        this.customValuesSet = true;
        this.customValue = value;
        this.customMinValue = min;
        this.customMaxValue = max;
        this.RecomposeCustomOverlays();
    }

    public void SetCustomMinMax(float min, float max)
    {
        this.customMinValue = min;
        this.customMaxValue = max;
        this.RecomposeCustomOverlays();
    }

    public override void Dispose()
    {
        base.Dispose();
        this.customBarTexture.Dispose();
        this.customFlashTexture.Dispose();
        this.customValueTexture.Dispose();
    }

    private float customMinValue;
    private float customMaxValue = 100f;
    private float customValue = 32f;
    private float customLineInterval = 10f;
    private double[] customColor;
    private bool customRightToLeft;
    private LoadedTexture customBarTexture;
    private LoadedTexture customFlashTexture;
    private LoadedTexture customValueTexture;
    private int customValueWidth;
    private int customValueHeight;
    public bool CustomShouldFlash;
    public float CustomFlashTime;
    public bool CustomShowValueOnHover = true;
    private bool customValuesSet;
    private bool customHideable;
    public StatbarValueDelegate customOnGetStatbarValue;
    public CairoFont customValueFont = CairoFont.WhiteSmallText().WithStroke(ColorUtil.BlackArgbDouble, 0.75);
    public static double CustomDefaultHeight = 8.0;
}