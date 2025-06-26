using Verse;

namespace LaptopBatteryStatus;

internal class LaptopBatteryStatusSettings : ModSettings
{
    public float AutosaveOn;
    public float CriticalOn = 0.1f;
    public bool GrayFull = true;
    public bool GreenCharging = true;
    public float WarningOn = 0.25f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref WarningOn, "warningOn", 0.25f);
        Scribe_Values.Look(ref CriticalOn, "criticalOn", 0.1f);
        Scribe_Values.Look(ref AutosaveOn, "autosaveOn");
        Scribe_Values.Look(ref GreenCharging, "greenCharging", true);
        Scribe_Values.Look(ref GrayFull, "grayFull", true);
    }
}