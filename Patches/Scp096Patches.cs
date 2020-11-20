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

    [HarmonyPatch(typeof(PlayableScps.Scp096), nameof(PlayableScps.Scp096.OnUpdate))]
    static class Scp096OnUpdatePatch
    {
        public static bool Prefix(PlayableScps.Scp096 __instance)
        {
            if (((PlayableScp)__instance).isLocalPlayer)
            {
                __instance._abilityManager.RunInputs();
                CursorManager.ChangeMouseVisibility(LockReason.Scp096, (__instance.Charging || (__instance.TryingNotToCry || __instance.PryingGate)) ? MouseVisibilityType.InvisibleButCantMove : MouseVisibilityType.Invisible, false);
                if (!__instance.PlayerState.IsOffensive() && !__instance._visionTargets.IsEmpty<TargetComponent>())
                {
                    foreach (TargetComponent component in __instance._visionTargets)
                    {
                        if (component != null)
                        {
                            component.IsTarget = false;
                        }
                    }
                    __instance._visionTargets.Clear();
                }
                if (__instance.TryingNotToCry && (__instance.Hub.fpc.PlySpeed != Vector2.zero))
                {
                    Scp096InputMessage message = new Scp096InputMessage
                    {
                        InputState = Scp096InputState.None
                    };
                    NetworkClient.Send<Scp096InputMessage>(message, 0);
                }
            }
            if (NetworkServer.active)
            {
                __instance.UpdateShield();
                __instance.UpdateEnrage();
                __instance.UpdateCharging();
                __instance.UpdatePry();
                foreach (GameObject obj2 in PlayerManager.players)
                {
                    CharacterClassManager manager = obj2.GetComponent<CharacterClassManager>();
                    if (manager == null || ((manager.CurClass != RoleType.Spectator) && !manager.IsAnyScp()))
                    {
                        Player player = Player.Get(obj2);
                        if (player != null && TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) &&
                            TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disable096Trigger)) continue;
                        VisionInformation visionInformation = __instance.GetVisionInformation(obj2);
                        if (visionInformation.Looking)
                        {
                            __instance.ParseVisionInformation(visionInformation);
                        }
                    }
                }
                foreach (KeyValuePair<GameObject, Door> pair in PlayableScps.Scp096.takenDoors)
                {
                    if (pair.Value.isOpen)
                    {
                        PlayableScps.Scp096 scp = PlayableScps.Scp096.Get096FromPlayerObject(pair.Key);
                        if (scp != null)
                        {
                            if (pair.Key == null)
                            {
                                PlayableScps.Scp096.takenDoors.Remove(pair.Key);
                            }
                            else
                            {
                                scp.ResetState();
                            }
                            break;
                        }
                    }
                }
            }
            return false;
        }
    }
}
