using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Grenades;
using HarmonyLib;
using UnityEngine;

namespace Subclass.Patches
{
    [HarmonyPatch(typeof(FragGrenade), nameof(FragGrenade.ServersideExplosion))]
    static class FragGrenadeServerSideExplosionPatch
    {
        public static bool Prefix(FragGrenade __instance)
        {
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

    [HarmonyPatch(typeof(FlashGrenade), nameof(FlashGrenade.ServersideExplosion))]
    static class FlashGrenadeServerSideExplosionPatch
    {
        public static bool Prefix(FlashGrenade __instance, ref bool __result)
        {
            foreach (GameObject obj2 in PlayerManager.players)
            {
                Player target = Player.Get(obj2);
                Vector3 position = ((EffectGrenade) __instance).transform.position;
                ReferenceHub hub = ReferenceHub.GetHub(obj2);
                Flashed effect = hub.playerEffectsController.GetEffect<Flashed>();
                Deafened deafened = hub.playerEffectsController.GetEffect<Deafened>();
                Log.Debug($"Target is: {target?.Nickname}", Subclass.Instance.Config.Debug);
                if ((effect != null) &&
                    ((((EffectGrenade)__instance).thrower != null)
                    && (__instance._friendlyFlash ||
                    effect.Flashable(ReferenceHub.GetHub(((EffectGrenade)__instance).thrower.gameObject), position, __instance._ignoredLayers))))
                {
                    if (!Tracking.PlayersWithSubclasses.ContainsKey(target) ||
                        !Tracking.PlayersWithSubclasses[target].Abilities.Contains(AbilityType.FlashImmune))
                    {
                        float num = __instance.powerOverDistance.Evaluate((float)(Vector3.Distance(obj2.transform.position, position) / ((position.y > 900f) ? __instance.distanceMultiplierSurface : __instance.distanceMultiplierFacility))) * __instance.powerOverDot.Evaluate(Vector3.Dot(hub.PlayerCameraReference.forward, (hub.PlayerCameraReference.position - position).normalized));
                        byte intensity = (byte)Mathf.Clamp(Mathf.RoundToInt((float)((num * 10f) * __instance.maximumDuration)), 1, 0xff);
                        if ((intensity >= effect.Intensity) && (num > 0f))
                        {
                            hub.playerEffectsController.ChangeEffectIntensity<Flashed>(intensity);
                            if (deafened != null)
                            {
                                hub.playerEffectsController.EnableEffect(deafened, num * __instance.maximumDuration, true);
                            }
                        }
                    }else
                    {
                        Concussed concussedEffect = hub.playerEffectsController.GetEffect<Concussed>();
                        concussedEffect.Intensity = 3;
                        hub.playerEffectsController.EnableEffect(concussedEffect, 5);
                        SinkHole sinkHoleEffect = hub.playerEffectsController.GetEffect<SinkHole>();
                        sinkHoleEffect.Intensity = 2;
                        hub.playerEffectsController.EnableEffect(sinkHoleEffect, 3);
                    }
                }
            }
            __result = ((EffectGrenade)__instance).ServersideExplosion();
            return false;
        }
    }

}
