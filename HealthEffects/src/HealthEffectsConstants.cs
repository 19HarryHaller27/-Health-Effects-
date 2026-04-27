namespace HealthEffects;

public static class HealthEffectsConstants
{
    /// <summary>Single stat layer code so all this mod’s modifiers are one removable layer family.</summary>
    public const string StatLayer = "healtheffects";

    /// <summary>0/1 on the player entity: 1 = currently in the ≤10% HP “critical” state (so we only send the warning once per dip, not every tick).</summary>
    public const string AttrLowHpLatch = "healtheffects_lowhp_latch";
}
