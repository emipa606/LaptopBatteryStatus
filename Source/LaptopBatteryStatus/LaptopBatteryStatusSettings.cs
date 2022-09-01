using Verse;

namespace LaptopBatteryStatus;

internal class LaptopBatteryStatusSettings : ModSettings
{
    public float criticalOn = 0.1f;
    public bool grayFull = true;
    public bool greenCharging = true;
    public float warningOn = 0.25f;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref warningOn, "warningOn", 0.25f);
        Scribe_Values.Look(ref criticalOn, "criticalOn", 0.1f);
        Scribe_Values.Look(ref greenCharging, "greenCharging", true);
        Scribe_Values.Look(ref grayFull, "grayFull", true);
    }
}