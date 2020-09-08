using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.Handlers
{
    public class Player
    {
        public void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Door.doorType == Door.DoorTypes.HeavyGate) {
                if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player))
                {
                    SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                    if (subClass.Abilities.Contains(AbilityType.PryGates))
                    {
                        if (Tracking.OnCooldown(ev.Player, AbilityType.PryGates, subClass))
                        {
                            float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.PryGates, subClass, Time.time);
                            ev.Player.Broadcast((ushort) Mathf.Clamp(timeLeft - timeLeft/4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "Pry gates").Replace("{seconds}", timeLeft.ToString()));
                        } else
                        {
                            Tracking.AddCooldown(ev.Player, AbilityType.PryGates);
                            ev.Door.PryGate();
                        }
                    }
                }
            }else if (!ev.IsAllowed && !ev.Door.Networkdestroyed && !ev.Door.Networklocked &&Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && 
                      Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassKeycardReaders))
            {
                Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to access door with permission level {ev.Door.PermissionLevels}", Subclass.Instance.Config.Debug);
                ev.IsAllowed = true;
            }
        }

        public void OnDied(DiedEventArgs ev)
        {
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Target)) Tracking.PlayersWithSubclasses.Remove(ev.Target);
            if (Tracking.Cooldowns.ContainsKey(ev.Target)) Tracking.Cooldowns.Remove(ev.Target);
            if (Tracking.FriendlyFired.Contains(ev.Target)) Tracking.FriendlyFired.RemoveAll(e => e == ev.Target);
        }

        public void OnShooting(ShootingEventArgs ev)
        {
            Exiled.API.Features.Player target = Exiled.API.Features.Player.Get(ev.Target);
            if (target != null && target.Team == ev.Shooter.Team)
            {
                if (Tracking.FriendlyFired.Contains(target) || (Tracking.PlayersWithSubclasses.ContainsKey(ev.Shooter) &&
                    !Tracking.PlayersWithSubclasses[target].BoolOptions["DisregardHasFF"] && 
                    Tracking.PlayersWithSubclasses[ev.Shooter].BoolOptions["HasFriendlyFire"]) ||
                    (Tracking.PlayersWithSubclasses.ContainsKey(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] && 
                    Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]))
                {
                    if (!Tracking.FriendlyFired.Contains(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]) 
                        Tracking.AddToFF(ev.Shooter);
                    ev.Shooter.IsFriendlyFireEnabled = true;
                    Timing.CallDelayed(0.1f, () =>
                    {
                        ev.Shooter.IsFriendlyFireEnabled = false;
                    });
                }else
                {
                    if (Tracking.PlayersWithSubclasses.ContainsKey(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
                    !Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]) {
                        ev.IsAllowed = false;
                    }
                }
            }
        }
    }
}
