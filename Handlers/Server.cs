using CustomPlayerEffects;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Grenades;
using MEC;
using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Exiled.Permissions.Extensions;
using EPlayer = Exiled.API.Features.Player;
using System.Collections;
using Exiled.API.Enums;
using System;
using Respawning;

namespace Subclass.Handlers
{
    public class Server
    {
        System.Random rnd = new System.Random();
        public void OnRoundStarted()
        {
            Tracking.RoundStartedAt = Time.time;
            Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () =>
            {
                Log.Debug("Round started!", Subclass.Instance.Config.Debug);
                foreach (EPlayer player in EPlayer.List)
                {
                    Tracking.MaybeAddRoles(player);
                }
                foreach(string message in Tracking.QueuedCassieMessages)
                {
                    Cassie.Message(message, true, false);
                    Log.Debug($"Sending message via cassie: {message}", Subclass.Instance.Config.Debug);
                }
                Tracking.QueuedCassieMessages.Clear();
            });
        }

        public void OnRoundEnded(RoundEndedEventArgs ev)
        {
            Tracking.PlayersWithSubclasses.Clear();
            Tracking.Cooldowns.Clear();
            Tracking.FriendlyFired.Clear();
            Tracking.PlayersThatBypassedTeslaGates.Clear();
            Tracking.PreviousRoles.Clear();
            Tracking.PlayersWithZombies.Clear();
            Tracking.PlayersThatHadZombies.Clear();
            Tracking.QueuedCassieMessages.Clear();
            Tracking.NextSpawnWave.Clear();
            Tracking.NextSpawnWaveGetsRole.Clear();
            Tracking.PlayersThatJustGotAClass.Clear();
            Tracking.SubClassesSpawned.Clear();
            Tracking.PreviousSubclasses.Clear();
            Tracking.PreviousBadges.Clear();
            API.EnableAllClasses();
        }

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            if (ev.Players.Count == 0 || !ev.IsAllowed) return;
            Team spawnedTeam = ev.NextKnownTeam == SpawnableTeamType.NineTailedFox ? Team.MTF : Team.CHI;
            if (!Tracking.NumSpawnWaves.ContainsKey(spawnedTeam)) Tracking.NumSpawnWaves.Add(spawnedTeam, 0);
            Tracking.NumSpawnWaves[spawnedTeam]++;
            Timing.CallDelayed(5f, () => // Clear them after the wave spawns instead.
            {
                Tracking.NextSpawnWave.Clear();
                Tracking.NextSpawnWaveGetsRole.Clear();
                Tracking.SpawnWaveSpawns.Clear();
            });
            bool ntfSpawning = ev.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox;
            if (!Subclass.Instance.Config.AdditiveChance)
            {
                List<RoleType> hasRole = new List<RoleType>();
                foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] &&
                (!e.IntOptions.ContainsKey("MaxSpawnPerRound") || Tracking.ClassesSpawned(e) < e.IntOptions["MaxSpawnPerRound"]) &&
                (ntfSpawning ? (e.AffectsRoles.Contains(RoleType.NtfCadet) || e.AffectsRoles.Contains(RoleType.NtfCommander) || 
                e.AffectsRoles.Contains(RoleType.NtfLieutenant)) : e.AffectsRoles.Contains(RoleType.ChaosInsurgency)) &&
                ((e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.BoolOptions["OnlyAffectsSpawnWave"]) ||
                (e.BoolOptions.ContainsKey("AffectsSpawnWave") && e.BoolOptions["AffectsSpawnWave"])) &&
                (!e.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.BoolOptions["WaitForSpawnWaves"]
                && Tracking.GetNumWavesSpawned(e.StringOptions.ContainsKey("WaitSpawnWaveTeam") ? (Team)Enum.Parse(typeof(Team), e.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.IntOptions["NumSpawnWavesToWait"]))))
                {
                    if ((ntfSpawning ? (subClass.AffectsRoles.Contains(RoleType.NtfCadet) ||
                    subClass.AffectsRoles.Contains(RoleType.NtfCommander) || subClass.AffectsRoles.Contains(RoleType.NtfLieutenant)) 
                    : subClass.AffectsRoles.Contains(RoleType.ChaosInsurgency)) && (rnd.NextDouble() * 100) < subClass.FloatOptions["ChanceToGet"])
                    {
                        if (ntfSpawning)
                        {
                            if (!hasRole.Contains(RoleType.NtfCadet) && subClass.AffectsRoles.Contains(RoleType.NtfCadet))
                            {
                                Tracking.NextSpawnWaveGetsRole.Add(RoleType.NtfCadet, subClass);
                                hasRole.Add(RoleType.NtfCadet);
                            }

                            if (!hasRole.Contains(RoleType.NtfLieutenant) && subClass.AffectsRoles.Contains(RoleType.NtfLieutenant))
                            {
                                Tracking.NextSpawnWaveGetsRole.Add(RoleType.NtfLieutenant, subClass);
                                hasRole.Add(RoleType.NtfLieutenant);
                            }

                            if (!hasRole.Contains(RoleType.NtfCommander) && subClass.AffectsRoles.Contains(RoleType.NtfCommander))
                            {
                                Tracking.NextSpawnWaveGetsRole.Add(RoleType.NtfCommander, subClass);
                                hasRole.Add(RoleType.NtfCommander);
                            }

                            if (hasRole.Count == 3) break;
                        }
                        else
                        {
                            if (subClass.AffectsRoles.Contains(RoleType.ChaosInsurgency))
                            {
                                Tracking.NextSpawnWaveGetsRole.Add(RoleType.ChaosInsurgency, subClass);
                                break;
                            }
                        }
                    }
                }
            }else
            {
                double num = (rnd.NextDouble() * 100);
                if (!ntfSpawning && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.ChaosInsurgency)) return;
                else if (ntfSpawning && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfCadet) &&
                    !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfCommander) && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfLieutenant)) 
                    return;

                if (!ntfSpawning)
                {
                    foreach (var possibity in Subclass.Instance.ClassesAdditive[RoleType.ChaosInsurgency].Where(e => e.Key.BoolOptions["Enabled"] &&
                    (!e.Key.IntOptions.ContainsKey("MaxSpawnPerRound") || Tracking.ClassesSpawned(e.Key) < e.Key.IntOptions["MaxSpawnPerRound"]) &&
                    ((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) || 
                    (e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"]))))
                    {
                        Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for next spawn wave", Subclass.Instance.Config.Debug);
                        if (num < possibity.Value)
                        {
                            Tracking.NextSpawnWaveGetsRole.Add(RoleType.ChaosInsurgency, possibity.Key);
                            break;
                        }
                        else
                        {
                            Log.Debug($"Next spawn wave did not get subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
                        }
                    }
                }else
                {
                    RoleType[] roles = { RoleType.NtfCommander, RoleType.NtfLieutenant, RoleType.NtfCadet };
                    foreach (RoleType role in roles)
                    {
                        foreach (var possibity in Subclass.Instance.ClassesAdditive[role].Where(e => e.Key.BoolOptions["Enabled"] &&
                        (!e.Key.IntOptions.ContainsKey("MaxSpawnPerRound") || Tracking.ClassesSpawned(e.Key) < e.Key.IntOptions["MaxSpawnPerRound"]) &&
                        ((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) || 
                        (e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"]))))
                        {
                            Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for next spawn wave", Subclass.Instance.Config.Debug);
                            if (num < possibity.Value)
                            {
                                Tracking.NextSpawnWaveGetsRole.Add(role, possibity.Key);
                                break;
                            }
                            else
                            {
                                Log.Debug($"Next spawn wave did not get subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
                            }
                        }
                    }
                }
            }
            Tracking.NextSpawnWave = ev.Players;
        }

        public void AttemptRevive(SendingConsoleCommandEventArgs ev, SubClass subClass, bool necro = false)
        {
            Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} attempt", Subclass.Instance.Config.Debug);
            AbilityType ability = necro ? AbilityType.Necromancy : AbilityType.Revive;
            if (Tracking.OnCooldown(ev.Player, ability, subClass))
            {
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} on cooldown", Subclass.Instance.Config.Debug);
                Tracking.DisplayCooldown(ev.Player, necro ? AbilityType.Necromancy : AbilityType.Revive, subClass, necro ? "necromancy" : "revive", Time.time);
                return;
            }

            List<Collider> colliders = Physics.OverlapSphere(ev.Player.Position, 3f).Where(e => e.gameObject.GetComponentInParent<Ragdoll>() != null).ToList();

            colliders.Sort((Collider x, Collider y) =>
            {
                return Vector3.Distance(x.gameObject.transform.position, ev.Player.Position).CompareTo(Vector3.Distance(y.gameObject.transform.position, ev.Player.Position));
            });

            if (colliders.Count == 0)
            {
                ev.Player.Broadcast(2, Subclass.Instance.Config.ReviveFailedNoBodyMessage);
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} overlap did not hit a ragdoll", Subclass.Instance.Config.Debug);
                return;
            }

            Ragdoll doll = colliders[0].gameObject.GetComponentInParent<Ragdoll>();
            if (doll.owner == null)
            {
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                ev.Player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
                return;
            }

            if (doll.owner.DeathCause.GetDamageType() == DamageTypes.Lure)
            {
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                ev.Player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
                return;
            }

            EPlayer owner = EPlayer.Get(colliders[0].gameObject.GetComponentInParent<Ragdoll>().owner.PlayerId);
            if (owner != null && !owner.IsAlive)
            {
                bool revived = false;
                if (!necro && Tracking.GetPreviousTeam(owner) != null &&
                Tracking.GetPreviousTeam(owner) == ev.Player.Team)
                {
                    if (Tracking.PlayersThatJustGotAClass.ContainsKey(owner)) Tracking.PlayersThatJustGotAClass[owner] = Time.time + 3f;
                    else Tracking.PlayersThatJustGotAClass.Add(owner, Time.time + 3f);

                    owner.SetRole((RoleType)Tracking.GetPreviousRole(owner), true);

                    if (Tracking.PreviousSubclasses.ContainsKey(owner) && Tracking.PreviousSubclasses[owner].AffectsRoles.Contains((RoleType)Tracking.GetPreviousRole(owner)))
                        Tracking.AddClass(owner, Tracking.PreviousSubclasses[owner], false, true);

                    owner.Inventory.Clear();
                    revived = true;
                }
                else if (necro)
                {
                    owner.Role = RoleType.Scp0492;
                    Tracking.AddZombie(ev.Player, owner);
                    owner.IsFriendlyFireEnabled = true;
                    revived = true;
                }
                if (revived)
                {
                    Timing.CallDelayed(0.2f, () =>
                    {
                        owner.ReferenceHub.playerMovementSync.OverridePosition(ev.Player.Position + new Vector3(0.3f, 1f, 0), 0, true);
                    });
                    UnityEngine.Object.DestroyImmediate(doll.gameObject, true);
                    Tracking.AddCooldown(ev.Player, ability);
                    Tracking.UseAbility(ev.Player, ability, subClass);
                    Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} succeeded", Subclass.Instance.Config.Debug);
                }
                else
                {
                    Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                    ev.Player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
                }
            }
            else
            {
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                ev.Player.Broadcast(2, Subclass.Instance.Config.CantReviveMessage);
            }
        }

        public void SpawnGrenade(ItemType type, EPlayer player, SubClass subClass)
        {
            // Credit to KoukoCocoa's AdminTools for the grenade spawn script below, I was lost. https://github.com/KoukoCocoa/AdminTools/
            GrenadeManager grenadeManager = player.ReferenceHub.gameObject.GetComponent<GrenadeManager>();
            GrenadeSettings settings = grenadeManager.availableGrenades.FirstOrDefault(g => g.inventoryID == type);
            Grenade grenade = UnityEngine.Object.Instantiate(settings.grenadeInstance).GetComponent<Grenade>();
            if (type == ItemType.GrenadeFlash) grenade.fuseDuration = subClass.FloatOptions.ContainsKey("FlashOnCommandFuseTimer") ? subClass.FloatOptions["FlashOnCommandFuseTimer"] : 0.3f;
            else grenade.fuseDuration = subClass.FloatOptions.ContainsKey("GrenadeOnCommandFuseTimer") ? subClass.FloatOptions["GrenadeOnCommandFuseTimer"] : 0.3f;
            grenade.FullInitData(grenadeManager, player.Position, Quaternion.Euler(grenade.throwStartAngle),
                grenade.throwLinearVelocityOffset, grenade.throwAngularVelocity);
            NetworkServer.Spawn(grenade.gameObject);
        }
    }
}
