using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using PlayableScps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.Patches
{
    [HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.AddTarget))]
    static class Scp096AddTargetPatch
    {
        public static bool Prefix(PlayableScps.Scp096 __instance, GameObject target)
        {
            if (!NetworkServer.active)
            {
                throw new InvalidOperationException("Called AddTarget from client.");
            }
            Player player = Player.Get(target);
            if (Tracking.PlayersWithSubclasses.ContainsKey(player) && Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disable096Trigger)) return false;
            ReferenceHub hub = ReferenceHub.GetHub(target);
            if (__instance.CanReceiveTargets && ((hub != null) && !__instance._targets.Contains(hub)))
            {
                if (!__instance._targets.IsEmpty<ReferenceHub>())
                {
                    __instance.AddReset();
                }
                __instance._targets.Add(hub);
                __instance.AdjustShield(200);
            }
            return false;
        }
    }
}
