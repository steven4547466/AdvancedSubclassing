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
using System;
using Exiled.Loader;
using System.Reflection;
using Subclass.Effects;
using Subclass.MonoBehaviours;

namespace Subclass.Handlers
{
    public class Player
    {

        System.Random rnd = new System.Random();

        public void OnSpawning(SpawningEventArgs ev)
        {
            bool wasLockedBefore = Round.IsLocked;
            Round.IsLocked = true;
            Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () =>
            {
                ev.Player.Scale = new Vector3(1, 1, 1);
                try
                {
                    Tracking.RemoveZombie(ev.Player);
                    Tracking.QueuedCassieMessages.Clear();
                    if (Tracking.NextSpawnWave.Contains(ev.Player) && Tracking.NextSpawnWaveGetsRole.ContainsKey(ev.Player.Role))
                    {
                        Tracking.AddClass(ev.Player, Tracking.NextSpawnWaveGetsRole[ev.Player.Role]);
                    }
                    else
                    {
                        if (!Tracking.PlayersWithSubclasses.ContainsKey(ev.Player))
                        {
                            if (Subclass.Instance.Scp035Enabled)
                            {
                                EPlayer scp035 = (EPlayer)Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                                Tracking.RemoveAndAddRoles(ev.Player, false, scp035?.Id == ev.Player.Id);
                            }
                            else Tracking.RemoveAndAddRoles(ev.Player, false, false);
                        }
                    }
                    foreach (string message in Tracking.QueuedCassieMessages)
                    {
                        Cassie.Message(message, true, false);
                        Log.Debug($"Sending message via cassie: {message}", Subclass.Instance.Config.Debug);
                    }
                    Tracking.CheckRoundEnd();
                }
                catch(Exception e)
                {
                    Log.Error(e);
                }
                Round.IsLocked = wasLockedBefore;
            });
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
			object afkComp = ev.Player.GameObject.GetComponent("AFKComponent");
            if (afkComp != null)
            {
                if (afkComp.GetType().GetMember("PlayerToReplace").Length > 0)
                {
                    EPlayer p = (EPlayer)((FieldInfo)afkComp.GetType().GetMember("PlayerToReplace")[0]).GetValue(afkComp);
                    if (p != null)
                    {
                        if (API.PlayerHasSubclass(ev.Player))
						{
                            SubClass subClass = API.GetPlayersSubclass(ev.Player);
                            if (!Tracking.PlayersThatJustGotAClass.ContainsKey(p)) Tracking.PlayersThatJustGotAClass.Add(p, Time.time + 5f);
                            else Tracking.PlayersThatJustGotAClass[p] = Time.time + 5f;
                            Timing.CallDelayed(1f, () =>
                            {
                                API.GiveClass(p, subClass, true);
                            });
                        }
                    }
                }
            }
        }

        public void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (Tracking.PlayersVenting.Contains(ev.Player))
			{
                ev.IsAllowed = false;
                ev.Player.Position += (ev.Player.GameObject.transform.forward * 3.5f);
                return;
			}

            if (ev.Door.doorType == Door.DoorTypes.HeavyGate && ((ev.Door.PermissionLevels != 0 || ev.Door.Networklocked) && !ev.Door.isOpen && ev.Player.CurrentItemIndex == -1)) {
                if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player))
                {
                    SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                    if (subClass.Abilities.Contains(AbilityType.PryGates))
                    {
                        if (!Tracking.CanUseAbility(ev.Player, AbilityType.PryGates, subClass))
                        {
                            Tracking.DisplayCantUseAbility(ev.Player, AbilityType.PryGates, subClass, "pry gates");
                            return;
                        }

                        if (Tracking.OnCooldown(ev.Player, AbilityType.PryGates, subClass))
                        {
                            Tracking.DisplayCooldown(ev.Player, AbilityType.PryGates, subClass, "pry gates", Time.time);
                        } else
                        {
                            Tracking.AddCooldown(ev.Player, AbilityType.PryGates);
                            Tracking.UseAbility(ev.Player, AbilityType.PryGates, subClass);
                            ev.Door.PryGate();
                        }
                    }
                }
            }else if (!ev.IsAllowed && !ev.Door.Networkdestroyed && !ev.Door.Networklocked && Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && 
                      Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassKeycardReaders))
            {
                SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                if (!Tracking.CanUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    Tracking.DisplayCantUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers");
                    return;
                }

                if (Tracking.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    Tracking.DisplayCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers", Time.time);
                }
                else
                {
                    Log.Debug($"Player with subclass {subClass.Name} has been allowed to access door with permission level {ev.Door.PermissionLevels}", Subclass.Instance.Config.Debug);
                    Tracking.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    Tracking.UseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass);
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
                if (!Tracking.CanUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    Tracking.DisplayCantUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers");
                    return;
                }

                if (Tracking.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    Tracking.DisplayCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers", Time.time);
                }
                else
                {
                    Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to access locked locker", Subclass.Instance.Config.Debug);
                    Tracking.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    Tracking.UseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass);
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
                if (!Tracking.CanUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    Tracking.DisplayCantUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers");
                    return;
                }

                if (Tracking.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    Tracking.DisplayCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers", Time.time);
                }
                else
                {
                    Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to access locked locker", Subclass.Instance.Config.Debug);
                    Tracking.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    Tracking.UseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass);
                    ev.IsAllowed = true;
                }
            }
        }

        public void OnDying(DyingEventArgs ev)
        {
            if (!Tracking.AllowedToDamage(ev.Target, ev.Killer))
            {
                Log.Debug("Not allowed to kill", Subclass.Instance.Config.Debug);
                ev.IsAllowed = false;
                return;
            }

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

            if (ev.Killer != ev.Target && ev.Killer.Role == RoleType.Scp0492 && Tracking.PlayersWithSubclasses.ContainsKey(ev.Killer)
                && Tracking.PlayersWithSubclasses[ev.Killer].Abilities.Contains(AbilityType.Infect))
            {
                SubClass killerSubclass = Tracking.PlayersWithSubclasses[ev.Killer];
                if (Tracking.OnCooldown(ev.Killer, AbilityType.Infect, killerSubclass))
                {
                    Log.Debug($"Player {ev.Killer.Nickname} failed to infect (on cooldown)", Subclass.Instance.Config.Debug);
                    Tracking.DisplayCooldown(ev.Killer, AbilityType.Infect, killerSubclass, "infect", Time.time);
                    return;
                }

                if (!Tracking.CanUseAbility(ev.Killer, AbilityType.Infect, killerSubclass))
                {
                    Tracking.DisplayCantUseAbility(ev.Killer, AbilityType.Infect, killerSubclass, "infect");
                    return;
                }
                if ((rnd.NextDouble() * 100) < (killerSubclass.FloatOptions.ContainsKey("InfectChance") ? killerSubclass.FloatOptions["InfectChance"] : 25))
                {
                    Tracking.AddCooldown(ev.Killer, AbilityType.Infect);
                    Tracking.UseAbility(ev.Killer, AbilityType.Infect, killerSubclass);
                    Vector3 pos = ev.Target.Position;
                    Timing.CallDelayed(killerSubclass.FloatOptions.ContainsKey("InfectDelay") ? killerSubclass.FloatOptions["InfectDelay"] : 10, () =>
                    {
                        if (ev.Target.IsAlive) return;
                        ev.Target.SetRole(RoleType.Scp0492, true);
                        ev.Target.ReferenceHub.playerMovementSync.OverridePosition(pos, 0f);
                    });
                }
            }

            Timing.CallDelayed(0.1f, () =>
            {
                Tracking.CheckRoundEnd();
            });
        }

        public void OnSpawningRagdoll(SpawningRagdollEventArgs ev)
        {
            Timing.CallDelayed(0.1f, () => 
            {
                foreach(Ragdoll doll in UnityEngine.Object.FindObjectsOfType<Ragdoll>())
				{
                    if (doll.owner.PlayerId == ev.PlayerId)
					{
                        if (Tracking.GetPreviousRole(ev.Owner) != null && !Tracking.RagdollRoles.ContainsKey(doll.netId))
                        {
                            Tracking.RagdollRoles.Add(doll.netId, (RoleType)Tracking.GetPreviousRole(ev.Owner));
                            break;
                        }
					}
				}
			});
        }

        //public void OnDied(DiedEventArgs ev)
        //{

        //}

        public void OnEscaping(EscapingEventArgs ev)
        {
            bool wasLockedBefore = Round.IsLocked;
            Round.IsLocked = true;
            ev.Player.Scale = new Vector3(1, 1, 1);
            bool cuffed = ev.Player.IsCuffed;
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player))
            {
                if (!cuffed && Tracking.PlayersWithSubclasses[ev.Player].EscapesAs[0] == ev.Player.Role
                    || Tracking.PlayersWithSubclasses[ev.Player].EscapesAs[1] == ev.Player.Role)
                {
                    ev.IsAllowed = false;
                    return;
                }
            }
            Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () => {
                if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player))
                {
                    if (!cuffed && Tracking.PlayersWithSubclasses[ev.Player].EscapesAs[0] != RoleType.None) ev.Player.Role = Tracking.PlayersWithSubclasses[ev.Player].EscapesAs[0];
                    else if (cuffed && Tracking.PlayersWithSubclasses[ev.Player].EscapesAs[1] != RoleType.None) ev.Player.Role = Tracking.PlayersWithSubclasses[ev.Player].EscapesAs[1];
                }
                Tracking.RemoveAndAddRoles(ev.Player, false, false, true);
                Round.IsLocked = wasLockedBefore;
            });
        }

        public void OnHurting(HurtingEventArgs ev)
        {
            if (!Tracking.AllowedToDamage(ev.Target, ev.Attacker))
            {
                Log.Debug("Not allowed to damage", Subclass.Instance.Config.Debug);
                ev.IsAllowed = false;
                return;
            }

            // This optimization probably saves a lot of cpu time, I'll do similar things for the other methods later...
            SubClass attackerClass = Tracking.PlayersWithSubclasses.ContainsKey(ev.Attacker) ? Tracking.PlayersWithSubclasses[ev.Attacker] : null;
            SubClass targetClass = Tracking.PlayersWithSubclasses.ContainsKey(ev.Target) ? Tracking.PlayersWithSubclasses[ev.Target] : null;

            if (ev.Attacker.Id != ev.Target.Id) ev.Attacker.ReferenceHub.playerEffectsController.DisableEffect<Scp268>();

            if (ev.DamageType != DamageTypes.Falldown && attackerClass != null && 
                (attackerClass.OnHitEffects.Count > 0))
            {
                foreach (string effect in attackerClass.OnHitEffects)
                {
                    Log.Debug($"Checking on hit effect: {effect}. For {ev.Attacker.Nickname} on {ev.Target.Nickname}", Subclass.Instance.Config.Debug);
                    if ((rnd.NextDouble() * 100) < attackerClass.FloatOptions[("OnHit" + effect + "Chance")])
                    {
                        Log.Debug($"Attempting to inflict on hit effect: {effect}. Inflicted by {ev.Attacker.Nickname} to {ev.Target.Nickname}", Subclass.Instance.Config.Debug);
                        ev.Target.ReferenceHub.playerEffectsController.EnableByString(effect,
                            attackerClass.FloatOptions.ContainsKey(("OnHit" + effect + "Duration")) ?
                            attackerClass.FloatOptions[("OnHit" + effect + "Duration")] : -1);
                        ev.Target.ReferenceHub.playerEffectsController.ChangeByString(effect, attackerClass.IntOptions.ContainsKey(("OnHit" + effect + "Intensity")) ?
                            (byte) attackerClass.IntOptions[("OnHit" + effect + "Intensity")] : (byte) 1);
                    }
                }
            }

            if (targetClass != null && (targetClass.OnDamagedEffects.ContainsKey(ev.DamageType.name.ToUpper().Replace("-", "").Replace(" ", ""))))
            {
                foreach (string effect in targetClass.OnDamagedEffects[ev.DamageType.name.ToUpper().Replace("-", "").Replace(" ", "")])
                {
                    Log.Debug($"Checking on hit damage: {effect} for {ev.Target.Nickname}", Subclass.Instance.Config.Debug);
                    if ((rnd.NextDouble() * 100) < targetClass.FloatOptions[("OnDamaged" + effect + "Chance")])
                    {
                        Log.Debug($"Attempting to inflict on damaged effect: {effect} to {ev.Target.Nickname}", Subclass.Instance.Config.Debug);
                        ev.Target.ReferenceHub.playerEffectsController.EnableByString(effect,
                            targetClass.FloatOptions.ContainsKey(("OnDamaged" + effect + "Duration")) ?
                            targetClass.FloatOptions[("OnDamaged" + effect + "Duration")] : -1);
                        ev.Target.ReferenceHub.playerEffectsController.ChangeByString(effect, attackerClass.IntOptions.ContainsKey(("OnDamaged" + effect + "Intensity")) ?
                            (byte)attackerClass.IntOptions[("OnDamaged" + effect + "Intensity")] : (byte)1);
                    }
                }
            }

            if (targetClass != null)
            {
                if (targetClass.Abilities.Contains(AbilityType.NoSCP207Damage) && ev.DamageType == DamageTypes.Scp207)
                    ev.IsAllowed = false;

                if (targetClass.Abilities.Contains(AbilityType.NoHumanDamage) && ev.DamageType.isWeapon)
                    ev.IsAllowed = false;

                if (targetClass.Abilities.Contains(AbilityType.NoSCPDamage) && ev.DamageType.isScp)
                    ev.IsAllowed = false;

                if (targetClass.Abilities.Contains(AbilityType.Nimble) && 
                    (rnd.NextDouble() * 100) < (targetClass.FloatOptions.ContainsKey("NimbleChance") ? 
                    targetClass.FloatOptions["NimbleChance"] : 15f))
                {
                    ev.IsAllowed = false;
                }
                if (Tracking.PlayersWithZombies.ContainsKey(ev.Target) && Tracking.PlayersWithZombies[ev.Target].Contains(ev.Attacker))
                    ev.IsAllowed = false;

                if (ev.DamageType == DamageTypes.Grenade && targetClass.Abilities.Contains(AbilityType.GrenadeImmune))
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

            if (targetClass != null && targetClass.Abilities.Contains(AbilityType.Regeneration))
            {
                if (ev.Target.ReferenceHub.playerEffectsController.AllEffects.ContainsKey(typeof(Regeneration)) && targetClass.FloatOptions.ContainsKey("RegenerationDisableDuration"))
                {
                    ((Regeneration)ev.Target.ReferenceHub.playerEffectsController.AllEffects[typeof(Regeneration)]).ActiveAt = Time.time + targetClass.FloatOptions["RegenerationDisableDuration"];
                }
            }

            if (ev.DamageType == DamageTypes.Falldown) return;
            if (attackerClass != null && attackerClass.Abilities.Contains(AbilityType.LifeSteal))
            {
                ev.Attacker.Health += Mathf.Clamp(ev.Amount * ((attackerClass.FloatOptions.ContainsKey("LifeStealPercent") ?
                    attackerClass.FloatOptions["LifeStealPercent"] : 2f) / 100), 0, ev.Attacker.MaxHealth - ev.Attacker.Health);
            }

            if (attackerClass != null && 
                attackerClass.FloatOptions.ContainsKey("OnHitDamageMultiplier"))
            {
                ev.Amount *= attackerClass.FloatOptions["OnHitDamageMultiplier"];
            }

        }

        public void OnShooting(ShootingEventArgs ev)
        {
            if (Tracking.PlayersInvisibleByCommand.Contains(ev.Shooter)) Tracking.PlayersInvisibleByCommand.Remove(ev.Shooter);
            if (Tracking.PlayersVenting.Contains(ev.Shooter)) Tracking.PlayersVenting.Remove(ev.Shooter);
            EPlayer target = EPlayer.Get(ev.Target);
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
            if (!Tracking.CanUseAbility(ev.Player, AbilityType.BypassTeslaGates, subClass))
            {
                Tracking.DisplayCantUseAbility(ev.Player, AbilityType.BypassTeslaGates, subClass, "bypass tesla gates");
                return;
            }

            if (Tracking.PlayerJustBypassedTeslaGate(ev.Player)) // The triggering tesla happens a lot, this allows the bypass to last 3 seconds.
            {
                ev.IsTriggerable = false;
                return;
            }
            if (Tracking.OnCooldown(ev.Player, AbilityType.BypassTeslaGates, subClass))
            {
                Tracking.DisplayCooldown(ev.Player, AbilityType.BypassTeslaGates, subClass, "bypass tesla gates", Time.time);
            }
            else
            {
                Log.Debug($"Player with subclass {Tracking.PlayersWithSubclasses[ev.Player].Name} has been allowed to bypass tesla gate", Subclass.Instance.Config.Debug);
                Tracking.AddCooldown(ev.Player, AbilityType.BypassTeslaGates);
                Tracking.UseAbility(ev.Player, AbilityType.BypassTeslaGates, subClass);
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

        public void OnUsingMedicalItem(UsingMedicalItemEventArgs ev)
        {
            if (ev.Item == ItemType.SCP268) return;
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.CantHeal))
            {
                ev.IsAllowed = false;
            }
        }

        public void OnEnteringPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && Tracking.PlayersWithSubclasses[ev.Player].RolesThatCantDamage.Contains(RoleType.Scp106)) ev.IsAllowed = false;
            EPlayer scp106 = null;
            if ((scp106 = EPlayer.List.FirstOrDefault(p => p.GameObject.GetComponent<Scp106PlayerScript>().iAm106)) != null)
            {
                if (Tracking.PlayersWithSubclasses.ContainsKey(scp106) && Tracking.PlayersWithSubclasses[scp106].CantDamageRoles.Contains(ev.Player.Role)) ev.IsAllowed = false;
            }
        }
    }
}
