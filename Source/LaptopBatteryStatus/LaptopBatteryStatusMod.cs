using System;
using Mlie;
using UnityEngine;
using Verse;

namespace LaptopBatteryStatus;

internal class LaptopBatteryStatusMod : Mod
{
    public static LaptopBatteryStatusSettings settings;

    private static string currentVersion;

    public LaptopBatteryStatusMod(ModContentPack content) : base(content)
    {
        settings = GetSettings<LaptopBatteryStatusSettings>();
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(
                ModLister.GetActiveModWithIdentifier("Mlie.LaptopBatteryStatus"));
    }

    public override string SettingsCategory()
    {
        return "Laptop Battery Status";
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var startColor = GUI.color;
        var listing_Standard = new Listing_Standard();
        listing_Standard.Begin(inRect);
        listing_Standard.Gap();
        var warningRect = listing_Standard.GetRect(30);
        GUI.color = Color.yellow;
        settings.warningOn = Widgets.HorizontalSlider(warningRect, settings.warningOn, 0.01f, 1f, false,
            "BS.warningOn".Translate(settings.warningOn.ToStringPercent()), 0.01f.ToStringPercent(),
            1f.ToStringPercent());
        settings.criticalOn = Math.Min(settings.criticalOn, settings.warningOn);

        listing_Standard.Gap();
        var criticalRect = listing_Standard.GetRect(30);
        GUI.color = Color.red;
        settings.criticalOn = Widgets.HorizontalSlider(criticalRect, settings.criticalOn, 0.01f, 1f,
            false,
            "BS.criticalOn".Translate(settings.criticalOn.ToStringPercent()), 0.01f.ToStringPercent(),
            1f.ToStringPercent());
        settings.warningOn = Math.Max(settings.criticalOn, settings.warningOn);

        listing_Standard.Gap();
        GUI.color = Color.green;
        listing_Standard.CheckboxLabeled("BS.greenoncharge".Translate(), ref settings.greenCharging);
        GUI.color = Color.gray;
        listing_Standard.CheckboxLabeled("BS.grayfull".Translate(), ref settings.grayFull);
        GUI.color = startColor;

        if (currentVersion != null)
        {
            listing_Standard.Gap();
            GUI.contentColor = Color.gray;
            listing_Standard.Label("BS.modversion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listing_Standard.End();

        settings.Write();
    }
}