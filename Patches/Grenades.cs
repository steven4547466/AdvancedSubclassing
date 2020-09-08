using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Grenades;
using HarmonyLib;

namespace Subclass.Patches
{
    [HarmonyPatch(typeof(FragGrenade), nameof(FragGrenade.ServersideExplosion))]
    static class FragGrenadeServerSideExplosionPatch
    {
        public static bool Prefix(FragGrenade __instance)
        {
            Log.Debug("In prefix");
            Player thrower = Player.Get(__instance.thrower.gameObject);
            if (Tracking.PlayersWithSubclasses.ContainsKey(thrower) && Tracking.PlayersWithSubclasses[thrower].Abilities.Contains(AbilityType.HealGrenadeFrag))
            {
                UnityEngine.Collider[] colliders = UnityEngine.Physics.OverlapSphere(__instance.transform.position, 4);
                Subclass.Instance.map.UpdateHealths(colliders, thrower, "HealGrenadeFragHealAmount");
                return false;
            }
            return true;
        }
    }
}
