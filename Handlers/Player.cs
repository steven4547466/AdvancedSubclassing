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
            Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () =>
            {
                if (!Subclass.Instance.RealisticSizesEnabled) ev.Player.Scale = new Vector3(1, 1, 1);
                try
                {
                    TrackingAndMethods.RemoveZombie(ev.Player);
                    TrackingAndMethods.QueuedCassieMessages.Clear();
                    if (TrackingAndMethods.NextSpawnWave.Contains(ev.Player) && TrackingAndMethods.NextSpawnWaveGetsRole.ContainsKey(ev.Player.Role))
                    {
                        TrackingAndMethods.AddClass(ev.Player, TrackingAndMethods.NextSpawnWaveGetsRole[ev.Player.Role]);
                    }
                    else
                    {
                        if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player))
                        {
                            if (Subclass.Instance.Scp035Enabled)
                            {
                                EPlayer scp035 = (EPlayer)Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                                TrackingAndMethods.RemoveAndAddRoles(ev.Player, false, scp035?.Id == ev.Player.Id);
                            }
                            else TrackingAndMethods.RemoveAndAddRoles(ev.Player, false, false);
                        }
                    }
                    foreach (string message in TrackingAndMethods.QueuedCassieMessages)
                    {
                        Cassie.Message(message, true, false);
                        Log.Debug($"Sending message via cassie: {message}", Subclass.Instance.Config.Debug);
                    }
                    TrackingAndMethods.CheckRoundEnd();
                }
                catch(Exception e)
                {
                    Log.Error(e);
                }
            });
        }

        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () =>
            {
                if (!ev.IsEscaped) TrackingAndMethods.RemoveAndAddRoles(ev.Player);
            });
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
                            if (!TrackingAndMethods.PlayersThatJustGotAClass.ContainsKey(p)) TrackingAndMethods.PlayersThatJustGotAClass.Add(p, Time.time + 5f);
                            else TrackingAndMethods.PlayersThatJustGotAClass[p] = Time.time + 5f;
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
            if (TrackingAndMethods.PlayersVenting.Contains(ev.Player))
			{
                ev.IsAllowed = false;
                Vector3 newPos = ev.Player.Position + (ev.Player.GameObject.transform.forward * 3.5f);
                newPos.y = ev.Player.Position.y;
                ev.Player.Position = newPos;
                return;
			}

            if (ev.Door.doorType == Door.DoorTypes.HeavyGate && ((ev.Door.PermissionLevels != 0 || ev.Door.Networklocked) && !ev.Door.isOpen && ev.Player.CurrentItemIndex == -1)) {
                if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player))
                {
                    SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[ev.Player];
                    if (subClass.Abilities.Contains(AbilityType.PryGates))
                    {
                        if (!TrackingAndMethods.CanUseAbility(ev.Player, AbilityType.PryGates, subClass))
                        {
                            TrackingAndMethods.DisplayCantUseAbility(ev.Player, AbilityType.PryGates, subClass, "pry gates");
                            return;
                        }

                        if (TrackingAndMethods.OnCooldown(ev.Player, AbilityType.PryGates, subClass))
                        {
                            TrackingAndMethods.DisplayCooldown(ev.Player, AbilityType.PryGates, subClass, "pry gates", Time.time);
                        } else
                        {
                            TrackingAndMethods.AddCooldown(ev.Player, AbilityType.PryGates);
                            TrackingAndMethods.UseAbility(ev.Player, AbilityType.PryGates, subClass);
                            ev.Door.PryGate();
                        }
                    }
                }
            }else if (!ev.IsAllowed && !ev.Door.Networkdestroyed && !ev.Door.Networklocked && TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player) && 
                      TrackingAndMethods.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassKeycardReaders))
            {
                SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[ev.Player];
                if (!TrackingAndMethods.CanUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    TrackingAndMethods.DisplayCantUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers");
                    return;
                }

                if (TrackingAndMethods.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    TrackingAndMethods.DisplayCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers", Time.time);
                }
                else
                {
                    Log.Debug($"Player with subclass {subClass.Name} has been allowed to access door with permission level {ev.Door.PermissionLevels}", Subclass.Instance.Config.Debug);
                    TrackingAndMethods.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    TrackingAndMethods.UseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass);
                    ev.IsAllowed = true;
                }
            }
        }

        public void OnInteractingLocker(InteractingLockerEventArgs ev)
        {
            if (ev.IsAllowed) return;
            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player) &&
                TrackingAndMethods.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassKeycardReaders))
            {
                SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[ev.Player];
                if (!TrackingAndMethods.CanUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    TrackingAndMethods.DisplayCantUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers");
                    return;
                }

                if (TrackingAndMethods.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    TrackingAndMethods.DisplayCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers", Time.time);
                }
                else
                {
                    Log.Debug($"Player with subclass {TrackingAndMethods.PlayersWithSubclasses[ev.Player].Name} has been allowed to access locked locker", Subclass.Instance.Config.Debug);
                    TrackingAndMethods.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    TrackingAndMethods.UseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass);
                    ev.IsAllowed = true;
                }
            }
        }

        public void OnUnlockingGenerator(UnlockingGeneratorEventArgs ev)
        {
            if (ev.IsAllowed) return;
            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player) &&
                TrackingAndMethods.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassKeycardReaders))
            {
                SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[ev.Player];
                if (!TrackingAndMethods.CanUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    TrackingAndMethods.DisplayCantUseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers");
                    return;
                }

                if (TrackingAndMethods.OnCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass))
                {
                    TrackingAndMethods.DisplayCooldown(ev.Player, AbilityType.BypassKeycardReaders, subClass, "bypass keycard readers", Time.time);
                }
                else
                {
                    Log.Debug($"Player with subclass {TrackingAndMethods.PlayersWithSubclasses[ev.Player].Name} has been allowed to access locked locker", Subclass.Instance.Config.Debug);
                    TrackingAndMethods.AddCooldown(ev.Player, AbilityType.BypassKeycardReaders);
                    TrackingAndMethods.UseAbility(ev.Player, AbilityType.BypassKeycardReaders, subClass);
                    ev.IsAllowed = true;
                }
            }
        }

        public void OnDying(DyingEventArgs ev)
        {
            if (!TrackingAndMethods.AllowedToDamage(ev.Target, ev.Killer))
            {
                Log.Debug("Not allowed to kill", Subclass.Instance.Config.Debug);
                ev.IsAllowed = false;
                return;
            }

            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Target) && TrackingAndMethods.PlayersWithSubclasses[ev.Target].Abilities.Contains(AbilityType.ExplodeOnDeath))
            {
                GrenadeManager grenadeManager = ev.Target.ReferenceHub.gameObject.GetComponent<GrenadeManager>();
                GrenadeSettings settings = grenadeManager.availableGrenades.FirstOrDefault(g => g.inventoryID == ItemType.GrenadeFrag);
                Grenade grenade = UnityEngine.Object.Instantiate(settings.grenadeInstance).GetComponent<Grenade>();
                grenade.fuseDuration = TrackingAndMethods.PlayersWithSubclasses[ev.Target].FloatOptions.ContainsKey("ExplodeOnDeathFuseTimer") ?
                    TrackingAndMethods.PlayersWithSubclasses[ev.Target].FloatOptions["ExplodeOnDeathFuseTimer"] : 2f;
                grenade.FullInitData(grenadeManager, ev.Target.Position, Quaternion.Euler(grenade.throwStartAngle),
                    grenade.throwLinearVelocityOffset, grenade.throwAngularVelocity, ev.Target.Team);
                NetworkServer.Spawn(grenade.gameObject);
            }

            SubClass subClass = TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Target) ? TrackingAndMethods.PlayersWithSubclasses[ev.Target] : null;
            TrackingAndMethods.AddPreviousTeam(ev.Target);
            TrackingAndMethods.RemoveZombie(ev.Target);
            TrackingAndMethods.RemoveAndAddRoles(ev.Target, true);

            if (ev.Killer != ev.Target && ev.Killer.Role == RoleType.Scp0492 && (subClass == null || !subClass.Abilities.Contains(AbilityType.CantBeInfected)) && 
                TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Killer) && TrackingAndMethods.PlayersWithSubclasses[ev.Killer].Abilities.Contains(AbilityType.Infect))
            {
                SubClass killerSubclass = TrackingAndMethods.PlayersWithSubclasses[ev.Killer];
                if (TrackingAndMethods.OnCooldown(ev.Killer, AbilityType.Infect, killerSubclass))
                {
                    Log.Debug($"Player {ev.Killer.Nickname} failed to infect (on cooldown)", Subclass.Instance.Config.Debug);
                    TrackingAndMethods.DisplayCooldown(ev.Killer, AbilityType.Infect, killerSubclass, "infect", Time.time);
                    return;
                }

                if (!TrackingAndMethods.CanUseAbility(ev.Killer, AbilityType.Infect, killerSubclass))
                {
                    TrackingAndMethods.DisplayCantUseAbility(ev.Killer, AbilityType.Infect, killerSubclass, "infect");
                    return;
                }
                if ((rnd.NextDouble() * 100) < (killerSubclass.FloatOptions.ContainsKey("InfectChance") ? killerSubclass.FloatOptions["InfectChance"] : 25))
                {
                    TrackingAndMethods.AddCooldown(ev.Killer, AbilityType.Infect);
                    TrackingAndMethods.UseAbility(ev.Killer, AbilityType.Infect, killerSubclass);
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
                TrackingAndMethods.CheckRoundEnd();
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
                        if (TrackingAndMethods.GetPreviousRole(ev.Owner) != null && !TrackingAndMethods.RagdollRoles.ContainsKey(doll.netId))
                        {
                            TrackingAndMethods.RagdollRoles.Add(doll.netId, (RoleType)TrackingAndMethods.GetPreviousRole(ev.Owner));
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
            if(!Subclass.Instance.RealisticSizesEnabled) ev.Player.Scale = new Vector3(1, 1, 1);
            bool cuffed = ev.Player.IsCuffed;
            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player))
            {
                if (!cuffed && TrackingAndMethods.PlayersWithSubclasses[ev.Player].EscapesAs[0] == ev.Player.Role
                    || TrackingAndMethods.PlayersWithSubclasses[ev.Player].EscapesAs[1] == ev.Player.Role)
                {
                    ev.IsAllowed = false;
                    return;
                }
            }
            Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () => {
                if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player))
                {
                    if (!cuffed && TrackingAndMethods.PlayersWithSubclasses[ev.Player].EscapesAs[0] != RoleType.None) ev.Player.SetRole(TrackingAndMethods.PlayersWithSubclasses[ev.Player].EscapesAs[0], false, true);
                    else if (cuffed && TrackingAndMethods.PlayersWithSubclasses[ev.Player].EscapesAs[1] != RoleType.None) ev.Player.SetRole(TrackingAndMethods.PlayersWithSubclasses[ev.Player].EscapesAs[1], false, true);
                }
                TrackingAndMethods.RemoveAndAddRoles(ev.Player, false, false, true);
            });
        }

        public void OnHurting(HurtingEventArgs ev)
        {
            if (ev.DamageType == DamageTypes.Scp207 && TrackingAndMethods.PlayersBloodLusting.Contains(ev.Target))
			{
                ev.IsAllowed = false;
                return;
			}
            SubClass attackerClass = TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Attacker) ? TrackingAndMethods.PlayersWithSubclasses[ev.Attacker] : null;
            SubClass targetClass = TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Target) ? TrackingAndMethods.PlayersWithSubclasses[ev.Target] : null;
            if (targetClass != null && targetClass.Abilities.Contains(AbilityType.CantBeInfected) && Subclass.Instance.Scp008XEnabled && ev.Attacker.Role == RoleType.Scp0492)
            {
                Timing.CallDelayed(0.1f, () => {
                    MethodBase method = Loader.Plugins.First(pl => pl.Name == "Scp008X")?.Assembly?.GetType("SCP008X.API.SCP008XAPI")?.GetMethod("Is008Infected", BindingFlags.Static | BindingFlags.Public);
                    if (method != null && (bool)method.Invoke(null, new[] { ev.Target }))
                    {
                        UnityEngine.Object.Destroy(ev.Target.ReferenceHub.GetComponent("SCP008X.Components.SCP008"));
                        ev.Target.ShowHint("", 0.1f);
                        Log.Debug("Prevented infection on " + ev.Target.Nickname, Subclass.Instance.Config.Debug);
                    }
                });
            }
            if (!TrackingAndMethods.AllowedToDamage(ev.Target, ev.Attacker))
            {
                Log.Debug("Not allowed to damage", Subclass.Instance.Config.Debug);
                ev.IsAllowed = false;
                return;
            }

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
                if (TrackingAndMethods.PlayersWithZombies.ContainsKey(ev.Target) && TrackingAndMethods.PlayersWithZombies[ev.Target].Contains(ev.Attacker))
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
            if (TrackingAndMethods.PlayersInvisibleByCommand.Contains(ev.Shooter)) TrackingAndMethods.PlayersInvisibleByCommand.Remove(ev.Shooter);
            if (TrackingAndMethods.PlayersVenting.Contains(ev.Shooter)) TrackingAndMethods.PlayersVenting.Remove(ev.Shooter);
            EPlayer target = EPlayer.Get(ev.Target);
            if (target != null)
            {
                if (TrackingAndMethods.PlayerHasFFToPlayer(ev.Shooter, target))
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
            if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player) || !TrackingAndMethods.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.BypassTeslaGates)) return;
            SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[ev.Player];
            if (!TrackingAndMethods.CanUseAbility(ev.Player, AbilityType.BypassTeslaGates, subClass))
            {
                TrackingAndMethods.DisplayCantUseAbility(ev.Player, AbilityType.BypassTeslaGates, subClass, "bypass tesla gates");
                return;
            }

            if (TrackingAndMethods.PlayerJustBypassedTeslaGate(ev.Player)) // The triggering tesla happens a lot, this allows the bypass to last 3 seconds.
            {
                ev.IsTriggerable = false;
                return;
            }
            if (TrackingAndMethods.OnCooldown(ev.Player, AbilityType.BypassTeslaGates, subClass))
            {
                TrackingAndMethods.DisplayCooldown(ev.Player, AbilityType.BypassTeslaGates, subClass, "bypass tesla gates", Time.time);
            }
            else
            {
                Log.Debug($"Player with subclass {TrackingAndMethods.PlayersWithSubclasses[ev.Player].Name} has been allowed to bypass tesla gate", Subclass.Instance.Config.Debug);
                TrackingAndMethods.AddCooldown(ev.Player, AbilityType.BypassTeslaGates);
                TrackingAndMethods.UseAbility(ev.Player, AbilityType.BypassTeslaGates, subClass);
                if (!TrackingAndMethods.PlayersThatBypassedTeslaGates.ContainsKey(ev.Player)) TrackingAndMethods.PlayersThatBypassedTeslaGates.Add(ev.Player, 0);
                TrackingAndMethods.PlayersThatBypassedTeslaGates[ev.Player] = Time.time;
                ev.IsTriggerable = false;
                //ev.Player.IsUsingStamina = false;
            }
        }

        public void OnEnteringFemurBreaker(EnteringFemurBreakerEventArgs ev)
        {
            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player) && 
                TrackingAndMethods.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.CantBeSacraficed)) ev.IsAllowed = false;
        }

        public void OnFailingEscapePocketDimension(FailingEscapePocketDimensionEventArgs ev)
        {
            if (EPlayer.List.Any(p => p.Role == RoleType.Scp106))
            {
                EPlayer scp106 = EPlayer.List.First(p => p.Role == RoleType.Scp106);
                if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(scp106) && TrackingAndMethods.PlayersWithSubclasses[scp106].Abilities.Contains(AbilityType.Zombie106))
                {
                    ev.IsAllowed = false;
                    ev.Player.SetRole(RoleType.Scp0492, true);
                    if (TrackingAndMethods.PlayersWithSubclasses[scp106].IntOptions.ContainsKey("Zombie106Health")) {
                        ev.Player.Health = TrackingAndMethods.PlayersWithSubclasses[scp106].IntOptions["Zombie106Health"];
                        ev.Player.MaxHealth = TrackingAndMethods.PlayersWithSubclasses[scp106].IntOptions["Zombie106Health"];
                    }
                    List<Room> rooms = EMap.Rooms.Where(r => r.Zone == Exiled.API.Enums.ZoneType.HeavyContainment).ToList();
                    ev.Player.Position = rooms[rnd.Next(rooms.Count)].Position + new Vector3(0, 1, 0);
                }
            }
        }

        public void OnInteracted(InteractedEventArgs ev)
        {
            if (TrackingAndMethods.PlayersInvisibleByCommand.Contains(ev.Player)) TrackingAndMethods.PlayersInvisibleByCommand.Remove(ev.Player);
        }

        public void OnUsingMedicalItem(UsingMedicalItemEventArgs ev)
        {
            if (ev.Item == ItemType.SCP268) return;
            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player) && TrackingAndMethods.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.CantHeal))
            {
                ev.IsAllowed = false;
            }
        }

        public void OnEnteringPocketDimension(EnteringPocketDimensionEventArgs ev)
        {
            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(ev.Player) && TrackingAndMethods.PlayersWithSubclasses[ev.Player].RolesThatCantDamage.Contains(RoleType.Scp106)) ev.IsAllowed = false;
            EPlayer scp106 = null;
            if ((scp106 = EPlayer.List.FirstOrDefault(p => p.GameObject.GetComponent<Scp106PlayerScript>().iAm106)) != null)
            {
                if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(scp106) && TrackingAndMethods.PlayersWithSubclasses[scp106].CantDamageRoles.Contains(ev.Player.Role)) ev.IsAllowed = false;
            }
        }
    }
}
