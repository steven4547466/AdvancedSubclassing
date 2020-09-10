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
    [HarmonyPatch(typeof(Scp173PlayerScript), nameof(Scp173PlayerScript.FixedUpdate))]
    static class Scp173Patches
    {
        public static bool Prefix(Scp173PlayerScript __instance)
        {
            __instance.DoBlinkingSequence();
            if (__instance.iAm173 && (((NetworkBehaviour)__instance).isLocalPlayer || NetworkServer.active))
            {
                __instance.AllowMove = true;
                using (List<GameObject>.Enumerator enumerator = PlayerManager.players.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Scp173PlayerScript component = enumerator.Current.GetComponent<Scp173PlayerScript>();
                        if (!component.SameClass && (component.LookFor173(((NetworkBehaviour)__instance).gameObject, true) && __instance.LookFor173(component.gameObject, false)))
                        {
                            Player player = Player.Get(component._hub);
                            if (player != null && Tracking.PlayersWithSubclasses.ContainsKey(player) &&
                                Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disable173Stop))
                            {
                                continue;
                            }
                            __instance.AllowMove = false;
                            break;
                        }
                    }
                }
            }
            return false;
        }
    }
}
