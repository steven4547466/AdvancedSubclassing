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

        System.Random rnd = new System.Random();

        public void OnSpawning(SpawningEventArgs ev)
        {
            Timing.CallDelayed(0.1f, () =>
            {
                if (Tracking.NextSpawnWave.Contains(ev.Player) && Tracking.NextSpawnWaveGetsRole.ContainsKey(ev.Player.Role))
                {
                    Tracking.RemoveAndAddRoles(ev.Player, true);
                    Subclass.Instance.server.AddClass(ev.Player, Tracking.NextSpawnWaveGetsRole[ev.Player.Role]);
                }
                else
                {
                    Tracking.RemoveAndAddRoles(ev.Player);
                }
            });

        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            //Timing.CallDelayed(0.1f, () =>
            //{
            //    Tracking.RemoveAndAddRoles(ev.Player);
            //});
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
            Tracking.RemoveZombie(ev.Target);
        }

        public void OnHurting(HurtingEventArgs ev)
        {
            if(ev.DamageType != DamageTypes.Falldown && Tracking.PlayersWithSubclasses.ContainsKey(ev.Attacker) && 
                Tracking.PlayersWithSubclasses[ev.Attacker].OnHitEffects.Count > 0)
            {
                foreach(string effect in Tracking.PlayersWithSubclasses[ev.Attacker].OnHitEffects)
                {
                    if ((rnd.NextDouble() * 100) < Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions[("OnHit" + effect + "Chance")])
                    {
                        ev.Target.ReferenceHub.playerEffectsController.EnableByString(effect,
                            Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions.ContainsKey(("OnHit" + effect + "Duration")) ?
                            Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions[("OnHit" + effect + "Duration")] : -1, true);
                    }
                }
            }

            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Target))
            {
                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.NoSCP207Damage) && ev.DamageType == DamageTypes.Scp207)
                    ev.Amount = 0;

                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.NoHumanDamage) && ev.Attacker.Team != Team.SCP)
                    ev.Amount = 0;

                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.NoSCPDamage) && ev.Attacker.Team == Team.SCP)
                    ev.Amount = 0;

                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.Nimble) && 
                    (rnd.NextDouble() * 100) < (Tracking.PlayersWithSubclasses[ev.Target].FloatOptions.ContainsKey("NimbleChance") ? 
                    Tracking.PlayersWithSubclasses[ev.Target].FloatOptions["NimbleChance"] : 15f))
                {
                    ev.Amount = 0;
                }
                if (Tracking.PlayersWithZombies.ContainsKey(ev.Target) && Tracking.PlayersWithZombies[ev.Target].Contains(ev.Attacker))
                    ev.Amount = 0;

                if (ev.DamageType == DamageTypes.Grenade && Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.GrenadeImmune))
                {
                    Concussed concussedEffect = ev.Target.ReferenceHub.playerEffectsController.GetEffect<Concussed>();
                    concussedEffect.Intensity = 3;
                    ev.Target.ReferenceHub.playerEffectsController.EnableEffect(concussedEffect, 8);
                    SinkHole sinkHoleEffect = ev.Target.ReferenceHub.playerEffectsController.GetEffect<SinkHole>();
                    sinkHoleEffect.Intensity = 2;
                    ev.Target.ReferenceHub.playerEffectsController.EnableEffect(sinkHoleEffect, 5);
                    ev.Amount = 0;
                }
            }


            Exiled.API.Features.Player target = ev.Target;
            if (target != null && target.Team == ev.Attacker.Team)
            {
                if (Tracking.PlayersWithZombies.Where(p => p.Value.Contains(ev.Target)).Count() > 0)
                {
                    ev.Attacker.IsFriendlyFireEnabled = true;
                    Timing.CallDelayed(0.1f, () =>
                    {
                        ev.Attacker.IsFriendlyFireEnabled = false;
                    });
                    ev.IsAllowed = true;
                    return;
                }

                if(Tracking.PlayersWithSubclasses.ContainsKey(ev.Attacker) && Tracking.PlayersWithSubclasses.ContainsKey(ev.Target) && 
                    Tracking.PlayersWithSubclasses[ev.Attacker].AdvancedFFRules.Contains(Tracking.PlayersWithSubclasses[ev.Target].Name))
                {
                    ev.Attacker.IsFriendlyFireEnabled = true;
                    Timing.CallDelayed(0.1f, () =>
                    {
                        ev.Attacker.IsFriendlyFireEnabled = false;
                    });
                    ev.IsAllowed = true;
                    return;
                }

                if (Tracking.FriendlyFired.Contains(target) || (Tracking.PlayersWithSubclasses.ContainsKey(ev.Attacker) &&
                    !Tracking.PlayersWithSubclasses[ev.Attacker].BoolOptions["DisregardHasFF"] &&
                    Tracking.PlayersWithSubclasses[ev.Attacker].BoolOptions["HasFriendlyFire"]) ||
                    (Tracking.PlayersWithSubclasses.ContainsKey(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
                    Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]))
                {
                    if (!Tracking.FriendlyFired.Contains(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"])
                        Tracking.AddToFF(ev.Attacker);
                    ev.Attacker.IsFriendlyFireEnabled = true;
                    Timing.CallDelayed(0.1f, () =>
                    {
                        ev.Attacker.IsFriendlyFireEnabled = false;
                    });
                    ev.IsAllowed = true;
                }
                else
                {
                    if (Tracking.PlayersWithSubclasses.ContainsKey(target) && !Tracking.PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
                    !Tracking.PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"])
                    {
                        ev.IsAllowed = false;
                    }
                }
            }

        }

        public void OnShooting(ShootingEventArgs ev)
        {
            // I'm sure this will be used eventually
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

        public void OnEnteringFemurBreaker(EnteringFemurBreakerEventArgs ev)
        {
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && 
                Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.CantBeSacraficed)) ev.IsAllowed = false;
        }
    }
}
