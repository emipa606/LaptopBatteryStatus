// ReSharper disable RedundantUsingDirective, needed for debugging

using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace LaptopBatteryStatus;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
    private static float cachedBatteryPercent;
    private static float lastBatteryPercent;
    private static BatteryStatus cachedBatteryStatus;
    private static int timeToUpdate;

    static HarmonyPatches()
    {
        cachedBatteryPercent = SystemInfo.batteryLevel;
        cachedBatteryStatus = SystemInfo.batteryStatus;
        timeToUpdate = 0;
        var harmony = new Harmony("Mlie.LaptopBatteryStatus");
        harmony.Patch(AccessTools.Method(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoDate)), null,
            new HarmonyMethod(typeof(HarmonyPatches), nameof(BatteryStatus_NearDatePostfix)));
    }

    private static void UpdateValuesIfNeeded()
    {
        timeToUpdate--;
        if (timeToUpdate > 0)
        {
            return;
        }

        timeToUpdate = 400;
        lastBatteryPercent = cachedBatteryPercent;
#if DEBUG
        cachedBatteryPercent -= 0.05f;
        if (cachedBatteryPercent <= 0)
        {
            cachedBatteryPercent = 1f;
        }

        var values = Enum.GetValues(typeof(BatteryStatus));
        var random = new Random();
        cachedBatteryStatus = (BatteryStatus)values.GetValue(random.Next(values.Length));
        if (LaptopBatteryStatusMod.settings.autosaveOn > cachedBatteryPercent &&
            LaptopBatteryStatusMod.settings.autosaveOn < lastBatteryPercent)
        {
            GameDataSaveLoader.SaveGame("BS.Filename".Translate(cachedBatteryPercent.ToStringPercent()));
        }

        return;
#endif
        cachedBatteryPercent = SystemInfo.batteryLevel;
        cachedBatteryStatus = SystemInfo.batteryStatus;
        if (LaptopBatteryStatusMod.settings.autosaveOn > cachedBatteryPercent &&
            LaptopBatteryStatusMod.settings.autosaveOn < lastBatteryPercent)
        {
            GameDataSaveLoader.SaveGame("BS.Filename".Translate(cachedBatteryPercent.ToStringPercent()));
        }
    }

    private static void BatteryStatus_NearDatePostfix(ref float curBaseY)
    {
        UpdateValuesIfNeeded();
        float rightMargin;
        Rect zlRect;
        Rect rect;
        var startColor = GUI.contentColor;
#if RELEASE
        if (cachedBatteryPercent < 0)
        {
            GUI.contentColor = Color.gray;
            rightMargin = 7f;
            zlRect = new Rect(UI.screenWidth - Alert.Width, curBaseY - 24f, Alert.Width, 24f);
            Text.Font = GameFont.Small;

            if (Mouse.IsOver(zlRect))
            {
                Widgets.DrawHighlight(zlRect);
            }

            GUI.BeginGroup(zlRect);

            Text.Anchor = TextAnchor.UpperRight;
            rect = zlRect.AtZero();
            rect.xMax -= rightMargin;

            Widgets.Label(rect, "BS.NoBattery".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.contentColor = startColor;
            GUI.EndGroup();

            curBaseY -= zlRect.height;
            return;
        }
#endif
        if (cachedBatteryPercent < LaptopBatteryStatusMod.settings.warningOn)
        {
            GUI.contentColor = Color.yellow;
        }

        if (cachedBatteryPercent < LaptopBatteryStatusMod.settings.criticalOn)
        {
            GUI.contentColor = Color.red;
        }

        switch (cachedBatteryStatus)
        {
            case BatteryStatus.Charging when LaptopBatteryStatusMod.settings.greenCharging:
                GUI.contentColor = Color.green;
                break;
            case BatteryStatus.Full when LaptopBatteryStatusMod.settings.grayFull:
                GUI.contentColor = Color.gray;
                break;
        }

        rightMargin = 7f;
        zlRect = new Rect(UI.screenWidth - Alert.Width, curBaseY - 24f, Alert.Width, 24f);
        Text.Font = GameFont.Small;

        if (Mouse.IsOver(zlRect))
        {
            Widgets.DrawHighlight(zlRect);
        }

        GUI.BeginGroup(zlRect);

        Text.Anchor = TextAnchor.UpperRight;
        rect = zlRect.AtZero();
        rect.xMax -= rightMargin;

        Widgets.Label(rect, "BS.CurrentPercent".Translate(cachedBatteryPercent.ToStringPercent()));
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.contentColor = startColor;
        GUI.EndGroup();
        if (cachedBatteryStatus != BatteryStatus.Unknown)
        {
            TooltipHandler.TipRegion(zlRect, $"BS.{cachedBatteryStatus.ToString()}".Translate());
        }

        curBaseY -= zlRect.height;
    }
}