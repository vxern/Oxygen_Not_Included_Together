using HarmonyLib;
using ONI_Together.Networking.OxySync.StateMachines;
using Shared.Profiling;

namespace ONI_Together.Patches.OxySync
{
    [HarmonyPatch(typeof(AirFilter), nameof(AirFilter.OnSpawn))]
    public static class AirFilter_OxySync_Patch
    {
        public static void Postfix(AirFilter __instance)
        {
            using var _ = Profiler.Scope();

            if (__instance.IsNullOrDestroyed())
                return;

            __instance.gameObject.AddOrGet<AirFilterSyncer>();
        }
    }
}
