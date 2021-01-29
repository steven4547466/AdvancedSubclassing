using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using GameCore;
using Grenades;
using HarmonyLib;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using System.Collections.Generic;
using UnityEngine;

namespace Subclass.Patches
{
	[HarmonyPatch(typeof(FlashGrenade), nameof(FlashGrenade.ServersideExplosion))]
	static class FlashGrenadeServerSideExplosionPatch
	{
		public static bool Prefix(FlashGrenade __instance, ref bool __result)
		{
			Exiled.API.Features.Log.Debug($"Flash grenade explosion", Subclass.Instance.Config.Debug);
			foreach (GameObject obj2 in PlayerManager.players)
			{
				Player target = Player.Get(obj2);
				Vector3 position = ((EffectGrenade)__instance).transform.position;
				ReferenceHub hub = ReferenceHub.GetHub(obj2);
				Flashed effect = hub.playerEffectsController.GetEffect<Flashed>();
				Deafened deafened = hub.playerEffectsController.GetEffect<Deafened>();
				Exiled.API.Features.Log.Debug($"Flash target is: {target?.Nickname}", Subclass.Instance.Config.Debug);
				if ((effect != null) &&
					((((EffectGrenade)__instance).thrower != null)
					&& (__instance._friendlyFlash ||
					effect.Flashable(ReferenceHub.GetHub(((EffectGrenade)__instance).thrower.gameObject), position, __instance._ignoredLayers))))
				{
					if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(target) ||
						!TrackingAndMethods.PlayersWithSubclasses[target].Abilities.Contains(AbilityType.FlashImmune))
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
					}
					else
					{
						Concussed concussedEffect = hub.playerEffectsController.GetEffect<Concussed>();
						concussedEffect.Intensity = 3;
						hub.playerEffectsController.EnableEffect(concussedEffect, 5);
						Disabled disabledEffect = hub.playerEffectsController.GetEffect<Disabled>();
						disabledEffect.Intensity = 2;
						hub.playerEffectsController.EnableEffect(disabledEffect, 5);
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
