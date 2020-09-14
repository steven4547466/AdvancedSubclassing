using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using GameCore;
using Grenades;
using HarmonyLib;
using UnityEngine;

namespace Subclass.Patches
{
    [HarmonyPatch(typeof(FragGrenade), nameof(FragGrenade.ServersideExplosion))]
    static class FragGrenadeServerSideExplosionPatch
    {
        public static bool Prefix(FragGrenade __instance, ref bool __result)
        {
            Player thrower = Player.Get(__instance.thrower.gameObject);
            if (Tracking.PlayersWithSubclasses.ContainsKey(thrower) && Tracking.PlayersWithSubclasses[thrower].Abilities.Contains(AbilityType.HealGrenadeFrag))
            {
                UnityEngine.Collider[] colliders = UnityEngine.Physics.OverlapSphere(__instance.transform.position, 4);
                Subclass.Instance.map.UpdateHealths(colliders, thrower, "HealGrenadeFragHealAmount");
                return false;
            }

            string str = (((Grenade)((EffectGrenade)__instance)).thrower != null) ? ((Grenade)((EffectGrenade)__instance)).thrower.hub.LoggedNameFromRefHub() : ((string)"(UNKNOWN)");
            string[] textArray1 = new string[] { "Player ", (string)str, "'s ", (string)((Grenade)((EffectGrenade)__instance)).logName, " grenade exploded." };
            ServerLogs.AddLog(ServerLogs.Modules.Logger, string.Concat((string[])textArray1), ServerLogs.ServerLogType.GameEvent, false);

            if (((EffectGrenade) __instance).serverGrenadeEffect != null)
            {
                Transform transform = ((Grenade)((EffectGrenade)__instance)).transform;
                Object.Instantiate<GameObject>(((EffectGrenade)__instance).serverGrenadeEffect, transform.position, transform.rotation);
            }

            Vector3 position = ((EffectGrenade)__instance).transform.position;
            int num = 0;
            Collider[] colliderArray = Physics.OverlapSphere(position, __instance.chainTriggerRadius, (int)__instance.damageLayerMask);
            int index = 0;
            while (index < colliderArray.Length)
            {
                Collider collider = colliderArray[index];
                BreakableWindow component = collider.GetComponent<BreakableWindow>();
                if (component != null)
                {
                    if ((component.transform.position - position).sqrMagnitude <= __instance.sqrChainTriggerRadius)
                    {
                        component.ServerDamageWindow(500f);
                    }
                }
                else
                {
                    Door componentInParent = collider.GetComponentInParent<Door>();
                    if (componentInParent != null)
                    {
                        if ((!componentInParent.GrenadesResistant && (!componentInParent.commandlock && (!componentInParent.decontlock && !componentInParent.lockdown))) && ((componentInParent.transform.position - position).sqrMagnitude <= __instance.sqrChainTriggerRadius))
                        {
                            componentInParent.DestroyDoor(true);
                        }
                    }
                    else if (((__instance.chainLengthLimit == -1) || (__instance.chainLengthLimit > ((EffectGrenade)__instance).currentChainLength)) && ((__instance.chainConcurrencyLimit == -1) || (__instance.chainConcurrencyLimit > num)))
                    {
                        Pickup componentInChildren = collider.GetComponentInChildren<Pickup>();
                        if ((componentInChildren != null) && __instance.ChangeIntoGrenade(componentInChildren))
                        {
                            num = (int)(num + 1);
                        }
                    }
                }
                index = (int)(index + 1);
            }
            Player pthrower = Player.Get(((EffectGrenade)__instance).thrower.gameObject);
            foreach (GameObject obj2 in PlayerManager.players)
            {
                if (!ServerConsole.FriendlyFire && ((obj2 != ((EffectGrenade)__instance).thrower.gameObject) && 
                    (!obj2.GetComponent<WeaponManager>().GetShootPermission(((EffectGrenade)__instance).throwerTeam, false))) 
                    && !Tracking.PlayerHasFFToPlayer(pthrower, Player.Get(obj2)))
                {
                    continue;
                }
                PlayerStats component = obj2.GetComponent<PlayerStats>();
                if ((component != null) && component.ccm.InWorld)
                {
                    float amount = (float)(__instance.damageOverDistance.Evaluate(Vector3.Distance(position, component.transform.position)) * (component.ccm.IsHuman() ? ConfigFile.ServerConfig.GetFloat("human_grenade_multiplier", 0.7f) : ConfigFile.ServerConfig.GetFloat("scp_grenade_multiplier", 1f)));
                    if (amount > __instance.absoluteDamageFalloff)
                    {
                        Exiled.API.Features.Log.Debug($"Attempting to hurt player {Player.Get(obj2)?.Nickname} with a grenade", Subclass.Instance.Config.Debug);
                        Transform[] grenadePoints = component.grenadePoints;
                        index = 0;
                        while (true)
                        {
                            if (index < grenadePoints.Length)
                            {
                                Transform transform = grenadePoints[index];
                                if (Physics.Linecast(position, transform.position, (int)__instance.hurtLayerMask))
                                {
                                    index = (int)(index + 1);
                                    continue;
                                }
                                component.HurtPlayer(new PlayerStats.HitInfo(amount, (((EffectGrenade)__instance).thrower != null) ? ((EffectGrenade)__instance).thrower.hub.LoggedNameFromRefHub() : ((string)"(UNKNOWN)"), DamageTypes.Grenade, ((EffectGrenade)__instance).thrower.hub.queryProcessor.PlayerId), obj2, false);
                            }
                            if (!component.ccm.IsAnyScp())
                            {
                                ReferenceHub hub = ReferenceHub.GetHub(obj2);
                                float duration = __instance.statusDurationOverDistance.Evaluate(Vector3.Distance(position, component.transform.position));
                                hub.playerEffectsController.EnableEffect(hub.playerEffectsController.GetEffect<Burned>(), duration, false);
                                hub.playerEffectsController.EnableEffect(hub.playerEffectsController.GetEffect<Concussed>(), duration, false);
                            }
                            break;
                        }
                    }
                }
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(FlashGrenade), nameof(FlashGrenade.ServersideExplosion))]
    static class FlashGrenadeServerSideExplosionPatch
    {
        public static bool Prefix(FlashGrenade __instance, ref bool __result)
        {
            Exiled.API.Features.Log.Debug($"Flash grenade explosion", Subclass.Instance.Config.Debug);
            foreach (GameObject obj2 in PlayerManager.players)
            {
                Player target = Player.Get(obj2);
                Vector3 position = ((EffectGrenade) __instance).transform.position;
                ReferenceHub hub = ReferenceHub.GetHub(obj2);
                Flashed effect = hub.playerEffectsController.GetEffect<Flashed>();
                Deafened deafened = hub.playerEffectsController.GetEffect<Deafened>();
                Exiled.API.Features.Log.Debug($"Flash target is: {target?.Nickname}", Subclass.Instance.Config.Debug);
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

            if (((EffectGrenade)__instance).serverGrenadeEffect != null)
            {
                Transform transform = ((Grenade)__instance).transform;
                Object.Instantiate<GameObject>(((EffectGrenade)__instance).serverGrenadeEffect, transform.position, transform.rotation);
            }

            string str = (((Grenade)__instance).thrower != null) ? ((Grenade)__instance).thrower.hub.LoggedNameFromRefHub() : ((string)"(UNKNOWN)");
            string[] textArray1 = new string[] { "Player ", (string)str, "'s ", (string)((Grenade)__instance).logName, " grenade exploded." };
            ServerLogs.AddLog(ServerLogs.Modules.Logger, string.Concat((string[])textArray1), ServerLogs.ServerLogType.GameEvent, false);

            __result = true;
            return false;
        }
    }

}
