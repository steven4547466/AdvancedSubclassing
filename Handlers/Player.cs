using CustomPlayerEffects;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Events.Handlers;
using MEC;
using System.Linq;
using UnityEngine;
using EPlayer = Exiled.API.Features.Player;
using EMap = Exiled.API.Features.Map;
using System.Collections.Generic;
using Grenades;
using Mirror;

namespace Subclass.Handlers
{
    public class Player
    {

        System.Random rnd = new System.Random();

        public void OnSpawning(SpawningEventArgs ev)
        {
            if (Tracking.PlayersInvisibleByCommand.Contains(ev.Player)) Tracking.PlayersInvisibleByCommand.Remove(ev.Player);
            Timing.CallDelayed(0.1f, () =>
            {
                Tracking.QueuedCassieMessages.Clear();
                if (Tracking.NextSpawnWave.Contains(ev.Player) && Tracking.NextSpawnWaveGetsRole.ContainsKey(ev.Player.Role))
                {
                    Tracking.RemoveAndAddRoles(ev.Player, true);
                    Tracking.AddClass(ev.Player, Tracking.NextSpawnWaveGetsRole[ev.Player.Role]);
                }
                else
                {
                    if (!Tracking.PlayersWithSubclasses.ContainsKey(ev.Player)) Tracking.RemoveAndAddRoles(ev.Player, false, Subclass.Instance.Scp035Enabled && scp035.API.Scp035Data.GetScp035()?.Id == ev.Player.Id);
                }
                foreach (string message in Tracking.QueuedCassieMessages)
                {
                    Cassie.Message(message, true, false);
                    Log.Debug($"Sending message via cassie: {message}", Subclass.Instance.Config.Debug);
                }
                Tracking.CheckRoundEnd();
            });

        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (Tracking.PlayersInvisibleByCommand.Contains(ev.Player)) Tracking.PlayersInvisibleByCommand.Remove(ev.Player);
            Tracking.RemoveZombie(ev.Player);
            Timing.CallDelayed(0.1f, () =>
            {
                Tracking.QueuedCassieMessages.Clear();
                Tracking.RemoveAndAddRoles(ev.Player, false, Subclass.Instance.Scp035Enabled && scp035.API.Scp035Data.GetScp035()?.Id == ev.Player.Id);
                foreach (string message in Tracking.QueuedCassieMessages)
                {
                    Cassie.Message(message, true, false);
                    Log.Debug($"Sending message via cassie: {message}", Subclass.Instance.Config.Debug);
                }
                Tracking.CheckRoundEnd();
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
            if (Tracking.PlayersInvisibleByCommand.Contains(ev.Target)) Tracking.PlayersInvisibleByCommand.Remove(ev.Target);
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Target) && Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.ExplodeOnDeath))
            {
                GrenadeManager grenadeManager = ev.Target.ReferenceHub.gameObject.GetComponent<GrenadeManager>();
                GrenadeSettings settings = grenadeManager.availableGrenades.FirstOrDefault(g => g.inventoryID == ItemType.GrenadeFrag);
                Grenade grenade = UnityEngine.Object.Instantiate(settings.grenadeInstance).GetComponent<Grenade>();
                grenade.fuseDuration = Tracking.PlayersWithSubclasses[ev.Target].FloatOptions.ContainsKey("ExplodeOnDeathFuseTimer") ? 
                    Tracking.PlayersWithSubclasses[ev.Target].FloatOptions["ExplodeOnDeathFuseTimer"] : 2f;
                grenade.FullInitData(grenadeManager, ev.Target.Position, Quaternion.Euler(grenade.throwStartAngle),
                    grenade.throwLinearVelocityOffset, grenade.throwAngularVelocity);
                NetworkServer.Spawn(grenade.gameObject);
            }

            Tracking.AddPreviousTeam(ev.Target);
            Tracking.RemoveZombie(ev.Target);
            Tracking.RemoveAndAddRoles(ev.Target, true);
            Tracking.CheckRoundEnd();
        }
        
        public void OnEscaping(EscapingEventArgs ev)
        {
            if (Tracking.PlayersInvisibleByCommand.Contains(ev.Player)) Tracking.PlayersInvisibleByCommand.Remove(ev.Player);
            Tracking.RemoveAndAddRoles(ev.Player, true);
        }

        public void OnHurting(HurtingEventArgs ev)
        {
            if (!Tracking.AllowedToDamage(ev.Target, ev.Attacker))
            {
                Log.Debug("Not allowed to damage", Subclass.Instance.Config.Debug);
                ev.IsAllowed = false;
                return;
            }
            if(ev.DamageType != DamageTypes.Falldown && Tracking.PlayersWithSubclasses.ContainsKey(ev.Attacker) && 
                (Tracking.PlayersWithSubclasses[ev.Attacker].OnHitEffects.Count > 0))
            {
                if (Tracking.PlayersWithSubclasses[ev.Attacker].OnHitEffects.Count > 0)
                {
                    foreach (string effect in Tracking.PlayersWithSubclasses[ev.Attacker].OnHitEffects)
                    {
                        if ((rnd.NextDouble() * 100) < Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions[("OnHit" + effect + "Chance")])
                        {
                            ev.Target.ReferenceHub.playerEffectsController.EnableByString(effect,
                                Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions.ContainsKey(("OnHit" + effect + "Duration")) ?
                                Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions[("OnHit" + effect + "Duration")] : -1);
                        }
                    }
                }
            }

            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Target))
            {
                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.NoSCP207Damage) && ev.DamageType == DamageTypes.Scp207)
                    ev.IsAllowed = false;

                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.NoHumanDamage) && ev.DamageType.isWeapon)
                    ev.IsAllowed = false;

                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.NoSCPDamage) && ev.DamageType.isScp)
                    ev.IsAllowed = false;

                if (Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.Nimble) && 
                    (rnd.NextDouble() * 100) < (Tracking.PlayersWithSubclasses[ev.Target].FloatOptions.ContainsKey("NimbleChance") ? 
                    Tracking.PlayersWithSubclasses[ev.Target].FloatOptions["NimbleChance"] : 15f))
                {
                    ev.IsAllowed = false;
                }
                if (Tracking.PlayersWithZombies.ContainsKey(ev.Target) && Tracking.PlayersWithZombies[ev.Target].Contains(ev.Attacker))
                    ev.IsAllowed = false;

                if (ev.DamageType == DamageTypes.Grenade && Tracking.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.GrenadeImmune))
                {
                    Concussed concussedEffect = ev.Target.ReferenceHub.playerEffectsController.GetEffect<Concussed>();
                    concussedEffect.Intensity = 3;
                    ev.Target.ReferenceHub.playerEffectsController.EnableEffect(concussedEffect, 8);
                    Disabled disabledEffect = ev.Target.ReferenceHub.playerEffectsController.GetEffect<Disabled>();
                    disabledEffect.Intensity = 2;
                    ev.Target.ReferenceHub.playerEffectsController.EnableEffect(disabledEffect, 5);
                    ev.IsAllowed = false;
                }
            }

            if (ev.DamageType == DamageTypes.Falldown) return;
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Attacker) && Tracking.PlayersWithSubclasses[ev.Attacker].Abilities.Contains(AbilityType.LifeSteal))
            {
                ev.Attacker.Health += Mathf.Clamp(ev.Amount * ((Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions.ContainsKey("LifeStealPercent") ?
                    Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions["LiftStealPercent"] : 2f) / 100), 0, ev.Attacker.MaxHealth);
            }

            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Attacker) && 
                Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions.ContainsKey("OnHitDamageMultiplier"))
            {
                ev.Amount *= Tracking.PlayersWithSubclasses[ev.Attacker].FloatOptions["OnHitDamageMultiplier"];
            }

        }

        public void OnShooting(ShootingEventArgs ev)
        {
            if (Tracking.PlayersInvisibleByCommand.Contains(ev.Shooter)) Tracking.PlayersInvisibleByCommand.Remove(ev.Shooter);
            Exiled.API.Features.Player target = Exiled.API.Features.Player.Get(ev.Target);
            if (target != null)
            {
                if (Tracking.PlayerHasFFToPlayer(ev.Shooter, target))
                {
                    Log.Debug($"Attacker: {ev.Shooter.Nickname} has been granted friendly fire", Subclass.Instance.Config.Debug);
                    ev.Shooter.IsFriendlyFireEnabled = true;
                    Timing.CallDelayed(0.1f, () =>
                    {
                        ev.Shooter.IsFriendlyFireEnabled = false;
                    });
                }else
                {
                    Log.Debug($"Attacker: {ev.Shooter.Nickname} has not been granted friendly fire", Subclass.Instance.Config.Debug);
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
                //ev.Player.IsUsingStamina = false;
            }
        }

        public void OnEnteringFemurBreaker(EnteringFemurBreakerEventArgs ev)
        {
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && 
                Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.CantBeSacraficed)) ev.IsAllowed = false;
        }

        public void OnFailingEscapePocketDimension(FailingEscapePocketDimensionEventArgs ev)
        {
            if (EPlayer.List.Any(p => p.Role == RoleType.Scp106))
            {
                EPlayer scp106 = EPlayer.List.First(p => p.Role == RoleType.Scp106);
                if (Tracking.PlayersWithSubclasses.ContainsKey(scp106) && Tracking.PlayersWithSubclasses[scp106].Abilities.Contains(AbilityType.Zombie106))
                {
                    ev.IsAllowed = false;
                    ev.Player.SetRole(RoleType.Scp0492, true);
                    if (Tracking.PlayersWithSubclasses[scp106].IntOptions.ContainsKey("Zombie106Health")) {
                        ev.Player.Health = Tracking.PlayersWithSubclasses[scp106].IntOptions["Zombie106Health"];
                        ev.Player.MaxHealth = Tracking.PlayersWithSubclasses[scp106].IntOptions["Zombie106Health"];
                    }
                    List<Room> rooms = EMap.Rooms.Where(r => r.Zone == Exiled.API.Enums.ZoneType.HeavyContainment).ToList();
                    ev.Player.Position = rooms[rnd.Next(rooms.Count)].Position + new Vector3(0, 1, 0);
                }
            }
        }

        public void OnInteracted(InteractedEventArgs ev)
        {
            if (Tracking.PlayersInvisibleByCommand.Contains(ev.Player)) Tracking.PlayersInvisibleByCommand.Remove(ev.Player);
        }
    }
}
