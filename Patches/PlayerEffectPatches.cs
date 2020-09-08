using CustomPlayerEffects;
using Exiled.API.Features;
using HarmonyLib;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.Patches
{
    [HarmonyPatch(typeof(PlayerEffect), nameof(PlayerEffect.ServerDisable))]
    static class PlayerEffectServerDisablePatch
    {
        public static bool Prefix(PlayerEffect __instance)
        {
            Player player = Player.Get(__instance.Hub);
            if (!Tracking.PlayersWithSubclasses.ContainsKey(player) || !Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.InvisibleUntilInteract)) return true;
            Scp268 scp268 = __instance.Hub.playerEffectsController.GetEffect<Scp268>();
            Log.Debug($"268 intensity: {scp268.Intensity}. This intensity: {__instance.Intensity}", Subclass.Instance.Config.Debug);
            Log.Debug($"268 time left: {scp268.TimeLeft}. This time left: {__instance.TimeLeft}", Subclass.Instance.Config.Debug);
            Log.Debug($"268 time between ticks: {scp268.TimeBetweenTicks}. This time between ticks: {__instance.TimeBetweenTicks}", Subclass.Instance.Config.Debug);
            if (scp268.Intensity > 0 && scp268.Intensity == __instance.Intensity && scp268.TimeLeft == __instance.TimeLeft && scp268.TimeBetweenTicks == __instance.TimeBetweenTicks) // At this point, we're relatively sure it's the same
            {
                float cooldown = Tracking.PlayersWithSubclasses[player].AbilityCooldowns[AbilityType.InvisibleUntilInteract];
                player.Broadcast((ushort) Mathf.Clamp(cooldown / 2, 0.5f, 3), Tracking.PlayersWithSubclasses[player].StringOptions["AbilityCooldownMessage"].Replace("{ability}", "invisibility").Replace("{seconds}", (cooldown).ToString()));
                Timing.CallDelayed(cooldown, () =>
                {
                    player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
                });
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Scp268), nameof(Scp268.PublicUpdate))]
    static class Scp268PublicUpdatePatch
    {
        public static bool Prefix(Scp268 __instance)
        {
            if (NetworkServer.active && ((PlayerEffect)__instance).Enabled)
            {
                __instance.curTime += Time.deltaTime;
                Player player = Player.Get(((PlayerEffect)__instance).Hub);
                if (!(Tracking.PlayersWithSubclasses.ContainsKey(player) && Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.InvisibleUntilInteract)) && __instance.curTime > 15f)
                {
                    ((PlayerEffect)__instance).ServerDisable();
                }
                bool flag = false;
                using (SyncList<Inventory.SyncItemInfo>.SyncListEnumerator enumerator = ((PlayerEffect)__instance).Hub.inventory.items.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.id != ItemType.SCP268)
                        {
                            continue;
                        }
                        flag = true;
                    }
                }
                //if (!flag)
                //{
                //    //((PlayerEffect)__instance).ServerDisable();
                //}
            }
            if (((PlayerEffect)__instance).Hub.inventory.isLocalPlayer)
            {
                if (((PlayerEffect)__instance).Enabled)
                {
                    __instance.animationTime += Time.deltaTime;
                    if (__instance.animationTime > 1f)
                    {
                        __instance.animationTime = 1f;
                    }
                }
                else
                {
                    __instance.animationTime -= Time.deltaTime * 2f;
                    if (__instance.animationTime < 0f)
                    {
                        __instance.animationTime = 0f;
                    }
                }
                if (__instance.prevAnim != __instance.animationTime)
                {
                    bool flag2;
                    __instance.prevAnim = __instance.animationTime;
                    CameraFilterPack_Colors_Adjust_ColorRGB effect = ((PlayerEffect)__instance).Hub.gfxController.CustomCameraEffects[1].Effect as CameraFilterPack_Colors_Adjust_ColorRGB;
                    effect.enabled = flag2 = __instance.animationTime > 0f;
                    CameraFilterPack_TV_Vignetting vignetting1 = ((PlayerEffect)__instance).Hub.gfxController.CustomCameraEffects[0].Effect as CameraFilterPack_TV_Vignetting;
                    vignetting1.enabled = flag2;
                    vignetting1.Vignetting = vignetting1.VignettingFull = __instance.animationTime;
                    vignetting1.VignettingColor = (Color)new Color32(0, 1, 2, 0xff);
                    effect.Blue = __instance.animationTime * 0.98f;
                    effect.Brightness = __instance.animationTime * -0.97f;
                    effect.Red = effect.Green = __instance.animationTime * 0.97f;
                }
            }
            return false;
        }
    }
}
