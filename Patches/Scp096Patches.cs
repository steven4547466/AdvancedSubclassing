using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using PlayableScps;
using PlayableScps.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Targeting;
using UnityEngine;

namespace Subclass.Patches
{
	//[HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.AddTarget))]
	//static class Scp096AddTargetPatch
	//{
	//    public static bool Prefix(PlayableScps.Scp096 __instance, GameObject target)
	//    {
	//        Log.Debug("Inside 096 patch", Subclass.Instance.Config.Debug);
	//        if (!NetworkServer.active)
	//        {
	//            throw new InvalidOperationException("Called AddTarget from client.");
	//        }
	//        Player player = Player.Get(target);
	//        if (Tracking.PlayersWithSubclasses.ContainsKey(player) && Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disable096Trigger)) return false;
	//        ReferenceHub hub = ReferenceHub.GetHub(target);
	//        if (__instance.CanReceiveTargets && ((hub != null) && !__instance._targets.Contains(hub)))
	//        {
	//            if (!__instance._targets.IsEmpty<ReferenceHub>())
	//            {
	//                __instance.AddReset();
	//            }
	//            __instance._targets.Add(hub);
	//            __instance.AdjustShield(200);
	//        }
	//        return false;
	//    }
	//}

	[HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.UpdateVision)), HarmonyPriority(Priority.First)]
	static class Scp096OnUpdatePatch
	{
		public static bool Prefix(PlayableScps.Scp096 __instance)
		{
			if (__instance._flash.Enabled)
			{
				return false;
			}
			Vector3 vector = __instance.Hub.transform.TransformPoint(PlayableScps.Scp096._headOffset);
			foreach (KeyValuePair<GameObject, ReferenceHub> keyValuePair in ReferenceHub.GetAllHubs())
			{
				ReferenceHub value = keyValuePair.Value;
				Player p = Player.Get(value);
				if (p != null && TrackingAndMethods.PlayersWithSubclasses.ContainsKey(p) &&
					TrackingAndMethods.PlayersWithSubclasses[p].Abilities.Contains(AbilityType.Disable096Trigger))
					continue;
				CharacterClassManager characterClassManager = value.characterClassManager;
				if (characterClassManager.CurClass != RoleType.Spectator && !(value == __instance.Hub) && !characterClassManager.IsAnyScp()
					&& Vector3.Dot((value.PlayerCameraReference.position - vector).normalized, __instance.Hub.PlayerCameraReference.forward) >= 0.1f)
				{
					VisionInformation visionInformation = VisionInformation.GetVisionInformation(value, vector, -0.1f, 60f, true, true, __instance.Hub.localCurrentRoomEffects);
					if (visionInformation.IsLooking)
					{
						float delay = visionInformation.LookingAmount / 0.25f * (visionInformation.Distance * 0.1f);
						if (!__instance.Calming)
						{
							__instance.AddTarget(value.gameObject);
						}
						if (__instance.CanEnrage && value.gameObject != null)
						{
							__instance.PreWindup(delay);
						}
					}
				}
			}
			return false;
		}
	}
}
