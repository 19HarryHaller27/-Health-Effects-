using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace HealthEffects;

public class HealthEffectsServerSystem : ModSystem
{
    private ICoreServerAPI? sapi;
    private long tickId = -1;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;
        tickId = api.Event.RegisterGameTickListener(OnServerTick, 1000, 0);
    }

    public override void Dispose()
    {
        if (sapi is not null)
        {
            if (tickId != -1)
            {
                sapi.Event.UnregisterGameTickListener(tickId);
                tickId = -1;
            }
            sapi = null;
        }
    }

    private void OnServerTick(float _dt)
    {
        ICoreServerAPI? api = sapi;
        if (api is null)
        {
            return;
        }
        foreach (IPlayer pl in api.Server.Players)
        {
            if (!(pl is IServerPlayer sp))
            {
                continue;
            }
            if (sp.Entity == null)
            {
                continue;
            }
            if (!HealthUtil.TryGetHealthRatio(sp.Entity, out float r))
            {
                // If we cannot read health, remove our layer so a stale modifier never "sticks" from a prior tick.
                ClearVigorLayer(sp.Entity);
                continue;
            }
            ApplyVigorStats(sp.Entity, r);
            float pct = r * 100f;
            bool nowCritical = pct <= 10f;
            var a = sp.Entity.WatchedAttributes;
            if (nowCritical)
            {
                if (a.GetInt(HealthEffectsConstants.AttrLowHpLatch, 0) == 0)
                {
                    sp.SendMessage(
                        GlobalConstants.GeneralChatGroup,
                        "You are in dire shape—wounds drag on you, and your strength barely answers.",
                        EnumChatType.Notification);
                    a.SetInt(HealthEffectsConstants.AttrLowHpLatch, 1);
                    a.MarkPathDirty(HealthEffectsConstants.AttrLowHpLatch);
                }
            }
            else
            {
                if (a.GetInt(HealthEffectsConstants.AttrLowHpLatch, 0) != 0)
                {
                    a.SetInt(HealthEffectsConstants.AttrLowHpLatch, 0);
                    a.MarkPathDirty(HealthEffectsConstants.AttrLowHpLatch);
                }
            }
        }
    }

    private static void ClearVigorLayer(Entity e)
    {
        string code = HealthEffectsConstants.StatLayer;
        e.Stats.Remove("walkspeed", code);
        e.Stats.Remove("miningSpeedMul", code);
        e.Stats.Remove("rangedWeaponsSpeed", code);
        e.Stats.Remove("rangedWeaponsAcc", code);
        e.Stats.Remove("bowDrawingStrength", code);
    }

    private static void ApplyVigorStats(Entity e, float healthRatio)
    {
        // Move: target multiplier = healthRatio  =>  modifier = ratio - 1  (1:1, uncapped; at 0.05 HP, ~0.05 move).
        float moveAdd = healthRatio - 1f;
        // Bonuses: +1% to each for every 10% HP  =>  bonus = ratio * 0.1  (trait-style +10% at full HP)
        float bonus = healthRatio * 0.1f;
        string code = HealthEffectsConstants.StatLayer;
        e.Stats.Set("walkspeed", code, moveAdd, false);
        e.Stats.Set("miningSpeedMul", code, bonus, false);
        e.Stats.Set("rangedWeaponsSpeed", code, bonus, false);
        e.Stats.Set("rangedWeaponsAcc", code, bonus, false);
        e.Stats.Set("bowDrawingStrength", code, bonus, false);
    }
}
