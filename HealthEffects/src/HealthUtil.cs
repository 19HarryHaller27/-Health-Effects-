using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;

namespace HealthEffects;

internal static class HealthUtil
{
    public static bool TryGetHealthRatio(Entity entity, out float ratio)
    {
        ratio = 1f;
        if (entity == null)
        {
            return false;
        }
        ITreeAttribute t = entity.WatchedAttributes.GetTreeAttribute("health");
        if (t == null)
        {
            return false;
        }
        float maxH = t.GetFloat("maxhealth", 0f);
        if (maxH <= 0f)
        {
            return false;
        }
        float cur = t.GetFloat("currenthealth", maxH);
        ratio = cur / maxH;
        if (ratio < 0f)
        {
            ratio = 0f;
        }
        if (ratio > 1f)
        {
            ratio = 1f;
        }
        return true;
    }
}
