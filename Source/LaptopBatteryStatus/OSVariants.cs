using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Verse;

namespace LaptopBatteryStatus;

internal static class OSVariants
{
    public static OSPlatform SteamOS { get; } = OSPlatform.Create("STEAMOS");

    public static OSPlatform SteamDeck { get; } =
        OSPlatform.Create("STEAMDECK"); // The SteamDeck's main OS, which is a varient of SteamOS

    // Steam's runtime system; depending on version this could be linker (LD_LIBARY_PATH) based or container based. This is what's normally visible to games.
    // See https://github.com/ValveSoftware/steam-runtime
    public static OSPlatform SteamLinuxRuntime { get; } = OSPlatform.Create("STEAMRT");

    public static OSPlatform GetRuntimeOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OSPlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OSPlatform.OSX;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var osRelease = ParseOSRelease("/etc/os-release");
                if (osRelease.GetValueOrDefault("ID") == "steamos")
                {
                    return osRelease.GetValueOrDefault("VARIANT_ID") == "steamdeck" ? SteamDeck : SteamOS;
                }

                if (osRelease.GetValueOrDefault("ID") == "steamrt")
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

        Log.Warning("[LaptopBatteryStatus] Unknown OS detected; battery status may not work.");
        return OSPlatform.Create("UNKNOWN");
    }

    private static Dictionary<string, string> ParseOSRelease(string path)
    {
        // Regex matches <optionally quoted string> = <optionally quoted string>
        var r = new Regex(@"""?([^""=]+)""?\s*=\s*""?([^""=]+)""?");

        var rc = new Dictionary<string, string>();
        foreach (var fullLine in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(fullLine))
            {
                continue;
            }

            var line = fullLine.TrimStart();
            if (line.StartsWith("#"))
            {
                continue;
            }

            var m = r.Match(line);
            if (m.Success)
            {
                rc.Add(m.Groups[1].Value, m.Groups[2].Value);
            }
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