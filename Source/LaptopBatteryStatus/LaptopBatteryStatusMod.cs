using System;
using Mlie;
using UnityEngine;
using Verse;

namespace LaptopBatteryStatus;

internal class LaptopBatteryStatusMod : Mod
{
    public static LaptopBatteryStatusSettings Settings;

    private static string currentVersion;

    public LaptopBatteryStatusMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LaptopBatteryStatusSettings>();
        currentVersion =
            VersionFromManifest.GetVersionFromModMetaData(content.ModMetaData);
    }

    public override string SettingsCategory()
    {
        return "Laptop Battery Status";
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var startColor = GUI.color;
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        listingStandard.Gap();
        var warningRect = listingStandard.GetRect(30);
        GUI.color = Color.yellow;
        Settings.WarningOn = Widgets.HorizontalSlider(warningRect, Settings.WarningOn, 0.01f, 1f, false,
            "BS.warningOn".Translate(Settings.WarningOn.ToStringPercent()), 0.01f.ToStringPercent(),
            1f.ToStringPercent());
        Settings.CriticalOn = Math.Min(Settings.CriticalOn, Settings.WarningOn);

        listingStandard.Gap();
        var criticalRect = listingStandard.GetRect(30);
        GUI.color = Color.red;
        Settings.CriticalOn = Widgets.HorizontalSlider(criticalRect, Settings.CriticalOn, 0.01f, 1f,
            false,
            "BS.criticalOn".Translate(Settings.CriticalOn.ToStringPercent()), 0.01f.ToStringPercent(),
            1f.ToStringPercent());
        Settings.WarningOn = Math.Max(Settings.CriticalOn, Settings.WarningOn);

        listingStandard.Gap();
        var autosaveRect = listingStandard.GetRect(30);
        GUI.color = startColor;
        Settings.AutosaveOn = Widgets.HorizontalSlider(autosaveRect, Settings.AutosaveOn, 0f, 1f,
            false,
            "BS.autosaveOn".Translate(Settings.AutosaveOn.ToStringPercent()), 0f.ToStringPercent(),
            1f.ToStringPercent());

        listingStandard.Gap();
        GUI.color = Color.green;
        listingStandard.CheckboxLabeled("BS.greenoncharge".Translate(), ref Settings.GreenCharging);
        GUI.color = Color.gray;
        listingStandard.CheckboxLabeled("BS.grayfull".Translate(), ref Settings.GrayFull);
        GUI.color = startColor;

        if (currentVersion != null)
        {
            listingStandard.Gap();
            GUI.contentColor = Color.gray;
            listingStandard.Label("BS.modversion".Translate(currentVersion));
            GUI.contentColor = Color.white;
        }

        listingStandard.End();

        Settings.Write();
    }
}