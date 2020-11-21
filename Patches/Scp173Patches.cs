using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.Patches
{
	// Still doesnt work
	//[HarmonyPatch(typeof(Scp173PlayerScript), nameof(Scp173PlayerScript.LookFor173))]
	//static class Scp173LookFor173Patch
 //   {
 //       public static bool Prefix(Scp173PlayerScript __instance, GameObject scp, bool angleCheck, ref bool __result)
 //       {
	//		try
	//		{
	//			Player p = Player.Get(scp);
	//			if (p != null && TrackingAndMethods.PlayersWithSubclasses.ContainsKey(p) &&
	//				TrackingAndMethods.PlayersWithSubclasses[p].Abilities.Contains(AbilityType.Disable173Stop))
	//			{
	//				__result = false;
	//				return false;
	//			}
	//			return true;
	//		}
	//		catch(Exception e)
	//		{
	//			Log.Error($"Error in 173 LookFor173 patch: {e}");
	//			return true;
	//		}
	//	}
 //   }
}
