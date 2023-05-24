// ReSharper disable RedundantUsingDirective, needed for debugging

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Random = System.Random;

namespace LaptopBatteryStatus;

internal static class OSVariants
{
    public static OSPlatform SteamOS { get; } = OSPlatform.Create("STEAMOS");
    public static OSPlatform SteamDeck { get; } = OSPlatform.Create("STEAMDECK"); // The SteamDeck's main OS, which is a varient of SteamOS

    // Steam's runtime system; depending on version this could be linker (LD_LIBARY_PATH) based or container based. This is what's normally visible to games.
    // See https://github.com/ValveSoftware/steam-runtime
    public static OSPlatform SteamLinuxRuntime { get; } = OSPlatform.Create("STEAMRT");

    public static OSPlatform GetRuntimeOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                Dictionary<string, string> osRelease = ParseOSRelease("/etc/os-release");
                if (osRelease.GetValueOrDefault("ID") == "steamos")
                {
                    if (osRelease.GetValueOrDefault("VARIANT_ID") == "steamdeck")
                        return SteamDeck;
                    else
                        return SteamOS;
                }
                else if (osRelease.GetValueOrDefault("ID") == "steamrt")
                {
                    return SteamLinuxRuntime;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[LaptopBatteryStatus] Error while parsing os-release; battery status may not work.");
                Log.Message(e.ToString());
            }
            return OSPlatform.Linux;
        }
        else
        {
            Log.Warning("[LaptopBatteryStatus] Unknown OS detected; battery status may not work.");
            return OSPlatform.Create("UNKNOWN");
        }
    }

    private static Dictionary<string, string> ParseOSRelease(string path)
    {
        // Regex matches <optionally quoted string> = <optionally quoted string>
        Regex r = new(@"""?([^""=]+)""?\s*=\s*""?([^""=]+)""?");

        Dictionary<string, string> rc = new();
        foreach (string fullLine in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(fullLine))
                continue;
            string line = fullLine.TrimStart();
            if (line.StartsWith("#"))
                continue;
            Match m = r.Match(line);
            if (m.Success)
                rc.Add(m.Groups[1].Value, m.Groups[2].Value);
        }
        return rc;
    }

    public static bool IsLinuxLike(this OSPlatform os)
    {
        return os.Equals(OSPlatform.Linux) ||
               os.Equals(SteamLinuxRuntime) ||
               os.Equals(SteamOS) ||
               os.Equals(SteamDeck);
    }
}

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
        Verse.Log.Message( "[LaptopBatteryStatus] RuntimeInformation.IsOSPlatform is Linux? " + RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
        Verse.Log.Message($"[LaptopBatteryStatus] RuntimeInformation.OSDescription is [{RuntimeInformation.OSDescription}]");
        Verse.Log.Message( "[LaptopBatteryStatus] Detected OS is " + runtimeOS.ToString());
#endif
        linuxBatteryPath = null;
        if (runtimeOS.IsLinuxLike())
        {
            // SteamDecks have their battery at /sys/class/power_supply/BAT1, which is weird.
            // The main system battery is usually at BAT0. Thus, we shouldn't hard code the path
            // since this might change in the future. (Besides, hardcoding is yucky.) Instead,
            // use the first BAT* path that contains the "files" that we need.
            // If we don't find any, no worries. We'll try the method used for all the other systems.
            foreach (string d in Directory.GetDirectories("/sys/class/power_supply/", "BAT*"))
            {
                string dir = d + "/";
#if DEBUG
                Log.Message($"[LaptopBatteryStatus] checking battery candidate [{dir}]");
#endif
                if (File.Exists(dir + "status") &&
                    File.Exists(dir + "capacity"))
                {
                    linuxBatteryPath = dir;
                    break;
                }

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
        if (linuxBatteryPath != null && runtimeOS == OSVariants.SteamLinuxRuntime )
        {
            try
            {
                cachedBatteryPercent = int.Parse(File.ReadAllText(linuxBatteryPath + "/capacity").Trim()) / 100f;
                string statusStr = File.ReadAllText(linuxBatteryPath + "/status").Trim();
                if (!Enum.TryParse(statusStr, true, out cachedBatteryStatus))
                {
                    Log.WarningOnce($"[LaptopBatteryStatus] Unrecognized battery status: {statusStr}", statusStr.GetHashCodeSafe());
                    cachedBatteryStatus = BatteryStatus.Unknown;
                }
                return;
            }
            catch (Exception e)
            {
                // If we fail, fall back to using Unity's built-in system. They might have fixed it by now.
                Log.Warning("[LaptopBatteryStatus] Exception while getting battery status for Linux system. Falling back to Unity method.");
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
        var random = new Random();
        cachedBatteryStatus = (BatteryStatus)values.GetValue(random.Next(values.Length));
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