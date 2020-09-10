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
    // I just don't know why it doesnt work.
    //[HarmonyPatch(typeof(Scp173PlayerScript), nameof(Scp173PlayerScript.LookFor173))]
    static class Scp173LookFor173Patch
    {
        public static bool Prefix(Scp173PlayerScript __instance, GameObject scp, bool angleCheck, ref bool __result)
        {
            Log.Debug("Inside 173 patch", Subclass.Instance.Config.Debug);
            //Player player = Player.Get(__instance.netIdentity);
            ReferenceHub hub = ((NetworkBehaviour)__instance).GetComponentInChildren<ReferenceHub>();
            Log.Debug(Player.Get(hub).Id);
            //Log.Debug($"Player is {player?.Nickname}", Subclass.Instance.Config.Debug);
            //if (player != null && Tracking.PlayersWithSubclasses.ContainsKey(player) && 
            //    Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disable173Stop))
            //{
            //    Log.Debug("173 look disabled automatcially", Subclass.Instance.Config.Debug);
            //    __result = false;
            //    return false;
            //}
            return true;
        }
    }
}
