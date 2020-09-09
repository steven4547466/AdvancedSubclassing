using CustomPlayerEffects;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using PlayableScps;
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

        public void OnSpawning(SpawningEventArgs ev)
        {
            //Timing.CallDelayed(0.1f, () =>
            //{
            //    Tracking.RemoveAndAddRoles(ev.Player);
            //});
            
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                Tracking.RemoveAndAddRoles(ev.Player);
            });
        }

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
                            ev.Player.Broadcast((ushort) Mathf.Clamp(timeLeft - timeLeft/4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "pry gates").Replace("{seconds}", timeLeft.ToString()));
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
                SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                if (Tracking.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, Time.time);
                    ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "bypass keycard readers").Replace("{seconds}", timeLeft.ToString()));
                }
                else
                {
                    Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to access door with permission level {ev.Door.PermissionLevels}", Subclass.Instance.Config.Debug);
                    Tracking.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    ev.IsAllowed = true;
                }
            }
        }

        public void OnInteractingLocker(InteractingLockerEventArgs ev)
        {
            if (ev.IsAllowed) return;
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) &&
                Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassKeycardReaders))
            {
                SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                if (Tracking.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, Time.time);
                    ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "bypass keycard readers").Replace("{seconds}", timeLeft.ToString()));
                }
                else
                {
                    Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to access locked locker", Subclass.Instance.Config.Debug);
                    Tracking.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    ev.IsAllowed = true;
                }
            }
        }

        public void OnUnlockingGenerator(UnlockingGeneratorEventArgs ev)
        {
            if (ev.IsAllowed) return;
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) &&
                Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassKeycardReaders))
            {
                SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                if (Tracking.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, Time.time);
                    ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "bypass keycard readers").Replace("{seconds}", timeLeft.ToString()));
                }
                else
                {
                    Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to access locked locker", Subclass.Instance.Config.Debug);
                    Tracking.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    ev.IsAllowed = true;
                }
            }
        }

        public void OnDied(DiedEventArgs ev)
        {
            Tracking.AddPreviousTeam(ev.Target);
            Tracking.RemoveAndAddRoles(ev.Target, true);
        }

        public void OnHurting(HurtingEventArgs ev)
        {
            
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

        public void OnTriggeringTesla(TriggeringTeslaEventArgs ev)
        {
            if (!Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) || !Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassTeslaGates)) return;
            SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
            if (Tracking.PlayerJustBypassedTeslaGate(ev.Player)) // The triggering tesla happens a lot, this allows the bypass to last 3 seconds.
            {
                ev.IsTriggerable = false;
                return;
            }
            if (Tracking.OnCooldown(ev.Player, AbilityType.BypassTeslaGates, subClass))
            {
                float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.BypassTeslaGates, subClass, Time.time);
                ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "bypass tesla gates").Replace("{seconds}", timeLeft.ToString()));
            }
            else
            {
                Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to bypass tesla gate", Subclass.Instance.Config.Debug);
                Tracking.AddCooldown(ev.Player, AbilityType.BypassTeslaGates);
                if (!Tracking.PlayersThatBypassedTeslaGates.ContainsKey(ev.Player)) Tracking.PlayersThatBypassedTeslaGates.Add(ev.Player, 0);
                Tracking.PlayersThatBypassedTeslaGates[ev.Player] = Time.time;
                ev.IsTriggerable = false;
                ev.Player.IsUsingStamina = false;
            }
        }
    }
}
