using Exiled.API.Features;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subclass.Patches
{
    [HarmonyPatch(typeof(PlayerInteract), nameof(PlayerInteract.CallCmdContain106))]
    static class PlayerInteractCallCmdContain106Patch
    {
        public static bool Prefix(PlayerInteract __instance)
        {
            Player player = Player.Get(__instance._hub);
            if (player != null)
            {
                if (Tracking.PlayersWithSubclasses.ContainsKey(player) && 
                    Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.CantActivateFemurBreaker)) return false;
            }
            return true;
        }
    }
}
