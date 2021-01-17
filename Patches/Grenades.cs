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
	[HarmonyPatch(typeof(FragGrenade), nameof(FragGrenade.ServersideExplosion))]
	static class FragGrenadeServerSideExplosionPatch
	{
		public static bool Prefix(FragGrenade __instance, ref bool __result)
		{
			Player thrower = Player.Get(__instance.thrower.gameObject);

			Dictionary<Player, float> damages = new Dictionary<Player, float>();

			Vector3 position = ((EffectGrenade)__instance).transform.position;
			int num = 0;
			Collider[] colliderArray = Physics.OverlapSphere(position, __instance.chainTriggerRadius, (int)__instance.damageLayerMask);
			int index = 0;

			foreach (GameObject obj2 in PlayerManager.players)
			{
				if (!ServerConsole.FriendlyFire && ((obj2 != ((EffectGrenade)__instance).thrower.gameObject) &&
					(!obj2.GetComponent<WeaponManager>().GetShootPermission(((EffectGrenade)__instance).throwerTeam, false))))
				{
					continue;
				}
				PlayerStats component = obj2.GetComponent<PlayerStats>();
				if ((component != null) && component.ccm.InWorld)
				{
					float amount = (float)(__instance.damageOverDistance.Evaluate(Vector3.Distance(position, component.transform.position)) * (component.ccm.IsHuman() ? ConfigFile.ServerConfig.GetFloat("human_grenade_multiplier", 0.7f) : ConfigFile.ServerConfig.GetFloat("scp_grenade_multiplier", 1f)));
					damages.Add(Player.Get(obj2), amount);
				}
			}

			var ev = new ExplodingGrenadeEventArgs(thrower, damages, true, __instance.gameObject);

			Exiled.Events.Handlers.Map.OnExplodingGrenade(ev);

			if (!ev.IsAllowed) return false;

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
					DoorVariant componentInParent = collider.GetComponentInParent<DoorVariant>();
					if (componentInParent != null && componentInParent is IDamageableDoor damageableDoor)
					{
						damageableDoor.ServerDamage(__instance.damageOverDistance.Evaluate(Vector3.Distance(position, componentInParent.transform.position)), DoorDamageType.Grenade);
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

			foreach (var item in damages)
			{
				if (item.Value > __instance.absoluteDamageFalloff)
				{
					PlayerStats component = item.Key.GameObject.GetComponent<PlayerStats>();
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
							component.HurtPlayer(new PlayerStats.HitInfo(item.Value, (((EffectGrenade)__instance).thrower != null) ? ((EffectGrenade)__instance).thrower.hub.LoggedNameFromRefHub() : ((string)"(UNKNOWN)"), DamageTypes.Grenade, ((EffectGrenade)__instance).thrower.hub.queryProcessor.PlayerId), item.Key.GameObject, false);
						}
						if (!component.ccm.IsAnyScp())
						{
							ReferenceHub hub = item.Key.ReferenceHub;
							float duration = __instance.statusDurationOverDistance.Evaluate(Vector3.Distance(position, component.transform.position));
							hub.playerEffectsController.EnableEffect(hub.playerEffectsController.GetEffect<Burned>(), duration, false);
							hub.playerEffectsController.EnableEffect(hub.playerEffectsController.GetEffect<Concussed>(), duration, false);
						}
						break;
					}
				}
			}

			//if (Tracking.PlayersWithSubclasses.ContainsKey(thrower) && Tracking.PlayersWithSubclasses[thrower].Abilities.Contains(AbilityType.HealGrenadeFrag))
			//{
			//	if (!Tracking.CanUseAbility(thrower, AbilityType.HealGrenadeFrag, Tracking.PlayersWithSubclasses[thrower]))
			//	{
			//		Tracking.DisplayCantUseAbility(thrower, AbilityType.HealGrenadeFrag, Tracking.PlayersWithSubclasses[thrower], "heal frag");
			//	}
			//	else
			//	{
			//		Tracking.UseAbility(thrower, AbilityType.HealGrenadeFrag, Tracking.PlayersWithSubclasses[thrower]);
			//		UnityEngine.Collider[] colliders = UnityEngine.Physics.OverlapSphere(__instance.transform.position, 4);
			//		Subclass.Instance.map.UpdateHealths(colliders, thrower, "HealGrenadeFragHealAmount");
			//		return false;
			//	}
			//}

			string str = (((Grenade)((EffectGrenade)__instance)).thrower != null) ? ((Grenade)((EffectGrenade)__instance)).thrower.hub.LoggedNameFromRefHub() : ((string)"(UNKNOWN)");
			string[] textArray1 = new string[] { "Player ", (string)str, "'s ", (string)((Grenade)((EffectGrenade)__instance)).logName, " grenade exploded." };
			ServerLogs.AddLog(ServerLogs.Modules.Logger, string.Concat((string[])textArray1), ServerLogs.ServerLogType.GameEvent, false);

			if (((EffectGrenade)__instance).serverGrenadeEffect != null)
			{
				Transform transform = ((Grenade)((EffectGrenade)__instance)).transform;
				Object.Instantiate<GameObject>(((EffectGrenade)__instance).serverGrenadeEffect, transform.position, transform.rotation);
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
