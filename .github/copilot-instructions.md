# GitHub Copilot Instructions for LaptopBatteryStatus Mod

## Mod Overview and Purpose

The **Laptop Battery Status** mod for RimWorld displays your laptop's battery status within the game. This feature helps players manage their power levels and save their games before running out of battery. The mod allows for customizable color changes based on the battery percentage and charging status. Additionally, it offers an optional autosave feature when the battery percentage drops below a specified threshold.

## Key Features and Systems

- **Battery Status Display**: Shows the current battery percentage and charging status in the game interface.
- **Customizable Colors**: Users can configure different colors for various battery levels and charging states.
- **Autosave Functionality**: Saves the game automatically when the battery percentage is below a user-defined limit, with the save file named "Battery at X%".
- **Platform Support**: Utilizes Unity's built-in battery functions for most platforms. For SteamDeck running SteamOS, it leverages Linux kernel information via sysfs.
- **Mod Settings**: Allows users to adjust settings such as color thresholds and autosave percentage.
- **Translation Support**: Includes translations for Russian and Chinese languages.

## Coding Patterns and Conventions

- **Mod Structure**: The mod follows a standard structure with separate files for Harmony patches, mod-specific logic, settings, and OS-specific functions.
- **Harmony Patching**: Utilizes Harmony to patch existing RimWorld methods to inject custom battery status logic.
- **XML Integration**: Uses XML definitions to handle mod settings and update information.
- **Code Comments**: Extensive comments are provided to explain the purpose and functionality of each part of the code.
- **Naming Conventions**: Descriptive and consistent naming conventions are used for classes, methods, and variables to enhance readability and maintainability.

## XML Integration

The mod uses an XML file (`UpdateInfo.xml`) to store information about mod updates. This file is located in the `Defs` directory and is parsed to provide users with update notifications.

Example of `UpdateInfo.xml`:

xml
<TabulaRasa.UpdateDef>
  <ModIdentifier>Mlie.LaptopBatteryStatus</ModIdentifier>
  <Version>1.0</Version>
  <ReleaseNotes>
    <ReleaseNote>
      <Text>Initial release with basic battery status display.</Text>
    </ReleaseNote>
    <ReleaseNote>
      <Text>Added support for SteamDeck and Linux-like systems.</Text>
    </ReleaseNote>
  </ReleaseNotes>
</TabulaRasa.UpdateDef>


## Harmony Patching

Harmony is used to patch the `BatteryStatus_NearDatePostfix` method in RimWorld to inject custom battery status logic. This ensures that the mod can display accurate battery information and trigger autosave functionality when necessary.

Example of `HarmonyPatches.cs`:

csharp
using HarmonyLib;
using RimWorld;
using Verse;

namespace LaptopBatteryStatus
{
    [HarmonyPatch(typeof(SomeRimWorldClass), "BatteryStatus_NearDatePostfix")]
    public static class HarmonyPatches
    {
        public static void Postfix(ref string __result)
        {
            // Custom battery status logic
            __result = UpdateBatteryCache();
        }

        private static string UpdateBatteryCache()
        {
            // Update battery cache and return status
            return "Battery at 20%";
        }
    }
}


## Suggestions for Copilot

1. **Harmony Patching**: When working with Harmony patches, Copilot can suggest the correct method to patch and how to structure the patch method.
2. **XML Definitions**: When creating or modifying XML files, Copilot can assist in generating the correct XML structure and tags.
3. **Settings Handling**: Copilot can help in implementing and managing mod settings, including exposing data and handling user input.
4. **OS Variants**: For handling platform-specific logic, Copilot can suggest methods for detecting the operating system and implementing platform-specific code.
5. **Error Handling**: Copilot can assist in adding robust error handling to ensure the mod runs smoothly across different systems.

By following these guidelines and leveraging GitHub Copilot, developers can enhance the LaptopBatteryStatus mod with additional features, improvements, and better maintainability.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

