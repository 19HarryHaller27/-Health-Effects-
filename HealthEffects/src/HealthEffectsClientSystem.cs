using System;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;

namespace HealthEffects;

/// <summary>Attaches a standard shaded dialog panel to the character screen (same style as Environment/Stats), below the Environment block.</summary>
public class HealthEffectsClientSystem : ModSystem
{
    public const string ComposerKey = "healtheffects-vigor";

    private const string DynamicTextKey = "healtheffectstext";
    private const int TextClipW = 430;
    private const int TextClipH = 120;
    private const int YUnderTitle = 28;
    private const int TickMs = 200;
    private const int PanelGap = 12;

    private ICoreClientAPI? capi;
    private GuiDialogCharacterBase? charDlg;
    private long tickId = -1;
    private string lastVigorText = "";

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        capi = api;
        tickId = api.Event.RegisterGameTickListener(OnClientTick, TickMs);
    }

    public override void Dispose()
    {
        if (capi != null)
        {
            if (tickId != -1)
            {
                capi.Event.UnregisterGameTickListener(tickId);
                tickId = -1;
            }
        }

        DetachFromDialog();
    }

    private void OnClientTick(float dt)
    {
        if (capi == null)
        {
            return;
        }

        if (charDlg != null)
        {
            if (!IsDialogStillLoaded(charDlg))
            {
                DetachFromDialog();
            }
            else
            {
                if (!HasVigorComposer())
                {
                    TryComposeVigorPanel();
                }
                else
                {
                    TryRefreshVigorText();
                }
            }

            return;
        }

        for (int i = 0; i < capi.Gui.LoadedGuis.Count; i++)
        {
            if (capi.Gui.LoadedGuis[i] is not GuiDialogCharacterBase found)
            {
                continue;
            }

            charDlg = found;
            charDlg.ComposeExtraGuis += OnComposeExtraGuis;
            charDlg.OnClosed += OnCharDialogClosed;
            TryComposeVigorPanel();
            return;
        }
    }

    private bool HasVigorComposer()
    {
        if (charDlg is null)
        {
            return false;
        }

        if (charDlg.Composers is not { } c)
        {
            return false;
        }

        try
        {
            return c[ComposerKey] is not null;
        }
        catch
        {
            return false;
        }
    }

    private bool IsDialogStillLoaded(GuiDialogCharacterBase dlg)
    {
        if (capi == null)
        {
            return false;
        }

        for (int i = 0; i < capi.Gui.LoadedGuis.Count; i++)
        {
            if (ReferenceEquals(capi.Gui.LoadedGuis[i], dlg))
            {
                return true;
            }
        }

        return false;
    }

    private void OnCharDialogClosed()
    {
        lastVigorText = "";
        DetachFromDialog();
    }

    private void DetachFromDialog()
    {
        if (charDlg != null)
        {
            charDlg.ComposeExtraGuis -= OnComposeExtraGuis;
            charDlg.OnClosed -= OnCharDialogClosed;
        }

        charDlg = null;
    }

    private void OnComposeExtraGuis()
    {
        TryComposeVigorPanel();
    }

    private void TryComposeVigorPanel()
    {
        if (capi == null || charDlg == null)
        {
            return;
        }

        try
        {
            DoComposeVigorPanel();
        }
        catch (Exception ex)
        {
            capi.Logger.Error("healtheffects: failed to compose character vigor panel. " + ex);
        }
    }

    private void DoComposeVigorPanel()
    {
        if (capi == null || charDlg == null)
        {
            return;
        }

        var composers = charDlg.Composers;
        if (composers is null)
        {
            return;
        }

        if (composers["playercharacter"] is null)
        {
            return;
        }

        ElementBounds left = composers["playercharacter"]!.Bounds;
        ElementBounds? deathWounds = composers["deathwounds-wounds-panel"]?.Bounds;
        ElementBounds? stackBelow = deathWounds ?? composers["environment"]?.Bounds;

        Entity? ent = capi.World?.Player?.Entity;
        if (ent == null)
        {
            return;
        }

        string text = VigorTextForLocalEntity(ent);
        lastVigorText = text;

        CairoFont bodyFont = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.2);
        ElementBounds textBounds = ElementBounds.Fixed(0, YUnderTitle, TextClipW, TextClipH);
        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        textBounds = textBounds.WithParent(bgBounds);
        _ = bgBounds.WithChildren(textBounds);

        double offsetY;
        if (stackBelow != null)
        {
            offsetY = (stackBelow.renderY - left.renderY + stackBelow.OuterHeight) / RuntimeEnv.GUIScale + PanelGap;
        }
        else
        {
            offsetY = left.OuterHeight / RuntimeEnv.GUIScale + 8.0;
        }

        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.None)
            .WithFixedPosition(left.renderX / RuntimeEnv.GUIScale, left.renderY / RuntimeEnv.GUIScale + offsetY);

        GuiComposer compo = capi.Gui
            .CreateCompo(ComposerKey, dialogBounds)
            .AddShadedDialogBG(bgBounds, true, 0, 0.5f)
            .AddDialogTitleBar("Health & vigor", OnTitleCloseClicked)
            .BeginChildElements(bgBounds)
            .AddDynamicText(text, bodyFont, textBounds, DynamicTextKey)
            .EndChildElements()
            .Compose();

        composers[ComposerKey] = compo;
    }

    private void OnTitleCloseClicked()
    {
        charDlg?.TryClose();
    }

    private void TryRefreshVigorText()
    {
        if (capi == null || charDlg == null)
        {
            return;
        }

        Entity? ent = capi.World?.Player?.Entity;
        if (ent == null)
        {
            return;
        }

        string text = VigorTextForLocalEntity(ent);
        if (text == lastVigorText)
        {
            return;
        }

        lastVigorText = text;
        if (charDlg.Composers is not { } map)
        {
            return;
        }

        // Composers may be rebuilt; missing key or legacy dictionary access can throw — never break the game loop.
        GuiComposer? compo;
        try
        {
            compo = map[ComposerKey] as GuiComposer;
        }
        catch
        {
            return;
        }

        if (compo is null)
        {
            return;
        }

        if (compo.GetDynamicText(DynamicTextKey) is { } dtxt)
        {
            dtxt.SetNewText(text, false, true, false);
        }
    }

    private static string VigorTextForLocalEntity(Entity ent)
    {
        if (HealthUtil.TryGetHealthRatio(ent, out float ratio))
        {
            return BuildVigorText(ratio);
        }
        // Do not show a fake 100% — avoid misleading numbers if the health tree is missing (e.g. rare view states).
        return "Health: not shown\n(Awaiting health data, or this view has no local player entity.)";
    }

    private static string BuildVigorText(float healthRatio)
    {
        double hpPct = healthRatio * 100.0;
        double movePct = hpPct;
        double bonusPct = healthRatio * 10.0;
        var sb = new StringBuilder(220);
        sb.Append("Health (");
        sb.Append(hpPct.ToString("0.0", CultureInfo.InvariantCulture));
        sb.AppendLine("%)");
        sb.Append("Move speed: ");
        sb.Append(movePct.ToString("0.0", CultureInfo.InvariantCulture));
        sb.AppendLine("% of base");
        sb.Append("Well-being bonuses: Mining, ranged speed, accuracy, bow draw +");
        sb.Append(bonusPct.ToString("0.0", CultureInfo.InvariantCulture));
        sb.AppendLine("% (scales 1% per 10% HP).");
        return sb.ToString();
    }
}
