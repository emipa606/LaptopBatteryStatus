using System;
using System.IO;
using System.Runtime.InteropServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LaptopBatteryStatus;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
    private static float cachedBatteryPercent;
    private static float lastBatteryPercent;
    private static BatteryStatus cachedBatteryStatus;
    private static int timeToUpdate;
    private static string linuxBatteryPath;
    private static readonly OSPlatform runtimeOS;

    static HarmonyPatches()
    {
        runtimeOS = OSVariants.GetRuntimeOS();
#if DEBUG
        Log.Message(
            $"[LaptopBatteryStatus] RuntimeInformation.IsOSPlatform is Linux? {RuntimeInformation.IsOSPlatform(OSPlatform.Linux)}");
        Log.Message($"[LaptopBatteryStatus] RuntimeInformation.OSDescription is [{RuntimeInformation.OSDescription}]");
        Log.Message($"[LaptopBatteryStatus] Detected OS is {runtimeOS}");
#endif
        linuxBatteryPath = null;
        if (runtimeOS.IsLinuxLike())
        {
            // SteamDecks have their battery at /sys/class/power_supply/BAT1, which is weird.
            // The main system battery is usually at BAT0. Thus, we shouldn't hard code the path
            // since this might change in the future. (Besides, hardcoding is yucky.) Instead,
            // use the first BAT* path that contains the "files" that we need.
            // If we don't find any, no worries. We'll try the method used for all the other systems.
            foreach (var d in Directory.GetDirectories("/sys/class/power_supply/", "BAT*"))
            {
                var dir = $"{d}/";
#if DEBUG
                Log.Message($"[LaptopBatteryStatus] checking battery candidate [{dir}]");
#endif
                if (!File.Exists($"{dir}status") ||
                    !File.Exists($"{dir}capacity"))
                {
                    continue;
                }

                linuxBatteryPath = dir;
                break;
            }
#if DEBUG
            Log.Message($"[LaptopBatteryStatus] Using battery at path [{linuxBatteryPath}]");
#endif
        }

        UpdateBatteryCache();
        timeToUpdate = 0;
        var harmony = new Harmony("Mlie.LaptopBatteryStatus");
        harmony.Patch(AccessTools.Method(typeof(GlobalControlsUtility), nameof(GlobalControlsUtility.DoDate)), null,
            new HarmonyMethod(typeof(HarmonyPatches), nameof(BatteryStatus_NearDatePostfix)));
    }

    private static void UpdateBatteryCache()
    {
        // This may work for all Linux-like OSes but it is currently only tested on SteamDeck using steamrt
        // Consider modifying the following gate on the runtimeOS if it's shown to work elsewhere
        if (linuxBatteryPath != null && runtimeOS == OSVariants.SteamLinuxRuntime)
        {
            try
            {
                cachedBatteryPercent = int.Parse(File.ReadAllText($"{linuxBatteryPath}/capacity").Trim()) / 100f;
                var statusStr = File.ReadAllText($"{linuxBatteryPath}/status").Trim();
                if (Enum.TryParse(statusStr, true, out cachedBatteryStatus))
                {
                    return;
                }

                Log.WarningOnce($"[LaptopBatteryStatus] Unrecognized battery status: {statusStr}",
                    statusStr.GetHashCodeSafe());
                cachedBatteryStatus = BatteryStatus.Unknown;

                return;
            }
            catch (Exception e)
            {
                // If we fail, fall back to using Unity's built-in system. They might have fixed it by now.
                Log.Warning(
                    "[LaptopBatteryStatus] Exception while getting battery status for Linux system. Falling back to Unity method.");
                Log.Message(e.ToString());
                linuxBatteryPath = null;
            }
        }

        // If this isn't linux, or getting data from the linux system failed, use Unity
        cachedBatteryPercent = SystemInfo.batteryLevel;
        cachedBatteryStatus = SystemInfo.batteryStatus;
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
        cachedBatteryStatus = (BatteryStatus)values.GetValue(Rand.Range(0, values.Length - 1));
        if (LaptopBatteryStatusMod.settings.autosaveOn > cachedBatteryPercent &&
            LaptopBatteryStatusMod.settings.autosaveOn < lastBatteryPercent)
        {
            GameDataSaveLoader.SaveGame("BS.Filename".Translate(cachedBatteryPercent.ToStringPercent()));
        }
#else // if !DEBUG
        UpdateBatteryCache();
        if (LaptopBatteryStatusMod.settings.autosaveOn > cachedBatteryPercent &&
            LaptopBatteryStatusMod.settings.autosaveOn < lastBatteryPercent)
        {
            GameDataSaveLoader.SaveGame("BS.Filename".Translate(cachedBatteryPercent.ToStringPercent()));
        }
#endif
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