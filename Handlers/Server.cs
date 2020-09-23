using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using Exiled.Permissions.Commands.Permissions;
using Grenades;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using EPlayer = Exiled.API.Features.Player;

namespace Subclass.Handlers
{
    public class Server
    {

        public Dictionary<RoleType, int> rolesForClass = new Dictionary<RoleType, int>();
        System.Random rnd = new System.Random();
        public void OnRoundStarted()
        {
            Tracking.RoundStartedAt = Time.time;
            Timing.CallDelayed(0.1f, () =>
            {
                Log.Debug("Round started", Subclass.Instance.Config.Debug);
                foreach (Exiled.API.Features.Player player in Exiled.API.Features.Player.List)
                {
                    MaybeAddRoles(player);
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
        }



        public void MaybeAddRoles(EPlayer player)
        {
            if (!rolesForClass.ContainsKey(player.Role))
                rolesForClass.Add(player.Role, Subclass.Instance.Classes.Values.Count(e => e.BoolOptions["Enabled"] &&
                    e.AffectsRoles.Contains(player.Role)));
            if (rolesForClass[player.Role] > 0)
            {
                List<Team> teamsAlive = EPlayer.List.Select(p1 => p1.Team).ToList();
                teamsAlive.RemoveAll(t => t == Team.RIP);
                foreach (var item in Tracking.PlayersWithSubclasses.Where(s => s.Value.EndsRoundWith != Team.RIP))
                {
                    teamsAlive.Remove(item.Key.Team);
                    teamsAlive.Add(item.Value.EndsRoundWith);
                }

                teamsAlive.ForEach(t => {
                    if (t == Team.CDP) t = Team.CHI;
                    else if (t == Team.RSC) t = Team.MTF;
                    else if (t == Team.TUT) t = Team.SCP;
                });

                teamsAlive.Add(Team.SCP);

                if (!Subclass.Instance.Config.AdditiveChance)
                {
                    Log.Debug($"Evaluating possible subclasses for player with name {player.Nickname}", Subclass.Instance.Config.Debug);
                    foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] && e.AffectsRoles.Contains(player.Role) &&
                    (!e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") || !e.BoolOptions["OnlyAffectsSpawnWave"])))
                    {
                        Log.Debug($"Evaluating possible subclass {subClass.Name} for player with name {player.Nickname}", Subclass.Instance.Config.Debug);
                        if ((rnd.NextDouble() * 100) < subClass.FloatOptions["ChanceToGet"] && 
                            (!subClass.IntOptions.ContainsKey("MaxAlive") || 
                            Tracking.PlayersWithSubclasses.Where(e => e.Value.Name == subClass.Name).Count() < subClass.IntOptions["MaxAlive"]) && 
                            (subClass.EndsRoundWith == Team.RIP || teamsAlive.Contains(subClass.EndsRoundWith)))
                        {
                            Log.Debug($"{player.Nickname} attempting to be given subclass {subClass.Name}", Subclass.Instance.Config.Debug);
                            Tracking.AddClass(player, subClass);
                        }
                        else
                        {
                            Log.Debug($"Player with name {player.Nickname} did not get subclass {subClass.Name}", Subclass.Instance.Config.Debug);
                        }
                    }
                }
                else
                {
                    Log.Debug($"Evaluating possible subclasses for player with name {player.Nickname}", Subclass.Instance.Config.Debug);
                    double num = (rnd.NextDouble() * 100);

                    if (!Subclass.Instance.ClassesAdditive.ContainsKey(player.Role)) return;

                    foreach (var possibity in Subclass.Instance.ClassesAdditive[player.Role].Where(e => e.Key.BoolOptions["Enabled"] && e.Key.AffectsRoles.Contains(player.Role) &&
                    (!e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") || !e.Key.BoolOptions["OnlyAffectsSpawnWave"])))
                    {
                        Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for player with name {player.Nickname}", Subclass.Instance.Config.Debug);
                        if (num < possibity.Value && (!possibity.Key.IntOptions.ContainsKey("MaxAlive") || 
                            Tracking.PlayersWithSubclasses.Where(e => e.Value.Name == possibity.Key.Name).Count() < possibity.Key.IntOptions["MaxAlive"]) &&
                            (possibity.Key.EndsRoundWith == Team.RIP || teamsAlive.Contains(possibity.Key.EndsRoundWith)))
                        {
                            Log.Debug($"{player.Nickname} attempting to be given subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
                            Tracking.AddClass(player, possibity.Key);
                            break;
                        }else
                        {
                            Log.Debug($"Player with name {player.Nickname} did not get subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
                        }
                    }
                }
            }
            else
            {
                Log.Debug($"No subclasses for {player.Role}", Subclass.Instance.Config.Debug);
            }
        }

        

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            Tracking.NextSpawnWave.Clear();
            Tracking.NextSpawnWaveGetsRole.Clear();
            bool ntfSpawning = ev.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox;
            if (!Subclass.Instance.Config.AdditiveChance)
            {
                List<RoleType> hasRole = new List<RoleType>();
                foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => (ntfSpawning ? (e.AffectsRoles.Contains(RoleType.NtfCadet) ||
                    e.AffectsRoles.Contains(RoleType.NtfCommander) || e.AffectsRoles.Contains(RoleType.NtfLieutenant)) : e.AffectsRoles.Contains(RoleType.ChaosInsurgency))
                    && ((e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.BoolOptions["OnlyAffectsSpawnWave"]) ||
                    (e.BoolOptions.ContainsKey("AffectsSpawnWave") && e.BoolOptions["AffectsSpawnWave"]))))
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
                    foreach (var possibity in Subclass.Instance.ClassesAdditive[RoleType.ChaosInsurgency].Where(e => ((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave")
                        && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) || (e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"]))))
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
                        foreach (var possibity in Subclass.Instance.ClassesAdditive[role].Where(e => ((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave")
                            && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) || (e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"]))))
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

        public void OnSendingConsoleCommand(SendingConsoleCommandEventArgs ev)
        {
            Log.Debug($"Player {ev.Player.Nickname} sent a console command", Subclass.Instance.Config.Debug);
            ev.IsAllowed = false;
            switch(ev.Name)
            {
                case "revive":
                    Log.Debug($"Player {ev.Player.Nickname} is attempting to revive", Subclass.Instance.Config.Debug);
                    if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.Revive))
                    {
                        SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                        AttemptRevive(ev, subClass);
                    }else
                    {
                        ev.ReturnMessage = "You don't have the ability to revive!";
                    }
                    break;

                case "necro":
                    Log.Debug($"Player {ev.Player.Nickname} is attempting to necro", Subclass.Instance.Config.Debug);
                    if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.Necromancy))
                    {
                        SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                        AttemptRevive(ev, subClass, true);
                    }
                    else
                    {
                        ev.ReturnMessage = "You don't have the ability to necro!";
                    }
                    break;

                case "locate":
                    if (ev.Player.Role != RoleType.Scp93953 && ev.Player.Role != RoleType.Scp93989 && 
                        (!Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) || 
                        !Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.Scp939Vision)))
                    {
                        Log.Debug($"Player {ev.Player.Nickname} failed to echolocate", Subclass.Instance.Config.Debug);
                        ev.ReturnMessage = "You must be SCP-939 or have a subclass with its visuals to use this command";
                        return;
                    }

                    if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.Echolocate))
                    {
                        SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                        if (Tracking.OnCooldown(ev.Player, AbilityType.Echolocate, subClass))
                        {
                            Log.Debug($"Player {ev.Player.Nickname} failed to echolocate", Subclass.Instance.Config.Debug);
                            float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.Echolocate, subClass, Time.time);
                            ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "echolocation").Replace("{seconds}", timeLeft.ToString()));
                            return;
                        }
                        
                        Collider[] colliders = Physics.OverlapSphere(ev.Player.Position, subClass.FloatOptions.ContainsKey("EcholocateRadius") ? subClass.FloatOptions["EcholocateRadius"] : 10f);
                        
                        foreach(Collider PlayerCollider in colliders.Where(c => EPlayer.Get(c.gameObject) != null))
                        {
                            EPlayer.Get(PlayerCollider.gameObject).ReferenceHub.footstepSync?.CmdScp939Noise(100f);
                        }

                        Tracking.AddCooldown(ev.Player, AbilityType.Echolocate);
                        Log.Debug($"Player {ev.Player.Nickname} successfully used echolocate", Subclass.Instance.Config.Debug);
                    } 
                    break;

                case "noclip":
                    Log.Debug($"Player {ev.Player.Nickname} is attempting to noclip", Subclass.Instance.Config.Debug);
                    if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.NoClip))
                    {
                        SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                        if (Tracking.OnCooldown(ev.Player, AbilityType.NoClip, subClass))
                        {
                            Log.Debug($"Player {ev.Player.Nickname} failed to noclip - cooldown", Subclass.Instance.Config.Debug);
                            float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.NoClip, subClass, Time.time);
                            ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "echolocation").Replace("{seconds}", timeLeft.ToString()));
                            return;
                        }
                        bool previous = ev.Player.NoClipEnabled;
                        ev.Player.NoClipEnabled = !ev.Player.NoClipEnabled;
                        Tracking.AddCooldown(ev.Player, AbilityType.NoClip);
                        if(subClass.FloatOptions.ContainsKey("NoClipTime"))
                        {
                            Timing.CallDelayed(subClass.FloatOptions["NoClipTime"], () => 
                            {
                                if (ev.Player.NoClipEnabled != previous) ev.Player.NoClipEnabled = previous;
                            });
                        }
                        Log.Debug($"Player {ev.Player.Nickname} successfully noclipped", Subclass.Instance.Config.Debug);
                    }
                    else
                    {
                        ev.ReturnMessage = "You must have the noclip ability to use this command";
                        Log.Debug($"Player {ev.Player.Nickname} could not noclip", Subclass.Instance.Config.Debug);
                    }
                    break;

                case "flash":
                    if (Tracking.PlayersWithSubclasses.ContainsKey(ev.Player) && Tracking.PlayersWithSubclasses[ev.Player].Abilities.Contains(AbilityType.FlashOnCommand))
                    {
                        SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];
                        if (Tracking.OnCooldown(ev.Player, AbilityType.FlashOnCommand, subClass))
                        {
                            Log.Debug($"Player {ev.Player.Nickname} failed to flash on command", Subclass.Instance.Config.Debug);
                            float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.FlashOnCommand, subClass, Time.time);
                            ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "flash").Replace("{seconds}", timeLeft.ToString()));
                            return;
                        }

                        // Credit to KoukoCocoa's AdminTools for the grenade spawn script below, I was lost. https://github.com/KoukoCocoa/AdminTools/
                        GrenadeManager grenadeManager = ev.Player.ReferenceHub.gameObject.GetComponent<GrenadeManager>();
                        GrenadeSettings settings = grenadeManager.availableGrenades.FirstOrDefault(g => g.inventoryID == ItemType.GrenadeFlash);
                        Grenade grenade = UnityEngine.Object.Instantiate(settings.grenadeInstance).GetComponent<Grenade>();
                        grenade.fuseDuration = 0.3f;
                        grenade.FullInitData(grenadeManager, ev.Player.Position, Quaternion.Euler(grenade.throwStartAngle), 
                            grenade.throwLinearVelocityOffset, grenade.throwAngularVelocity);
                        NetworkServer.Spawn(grenade.gameObject);
                        Tracking.AddCooldown(ev.Player, AbilityType.FlashOnCommand);
                        Log.Debug($"Player {ev.Player.Nickname} successfully used flash on commad", Subclass.Instance.Config.Debug);
                    }
                    break;

                case "invis":
                    if (!Tracking.PlayersWithSubclasses.ContainsKey(ev.Player)) 
                    {
                        ev.ReturnMessage = "You must have the invisible on command ability to use this command";
                        Log.Debug($"Player {ev.Player.Nickname} could not go invisible on command", Subclass.Instance.Config.Debug);
                    }
                    Scp268 scp268 = ev.Player.ReferenceHub.playerEffectsController.GetEffect<Scp268>();
                    if (scp268 != null)
                    {
                        SubClass subClass = Tracking.PlayersWithSubclasses[ev.Player];

                        if (!subClass.Abilities.Contains(AbilityType.InvisibleOnCommand))

                        if (scp268.Enabled)
                        {
                            Log.Debug($"Player {ev.Player.Nickname} failed to go invisible on command", Subclass.Instance.Config.Debug);
                            ev.Player.Broadcast(3, "You're already invisible!");
                            return;
                        }

                        if (Tracking.OnCooldown(ev.Player, AbilityType.InvisibleOnCommand, subClass))
                        {
                            Log.Debug($"Player {ev.Player.Nickname} failed to go invisible on command", Subclass.Instance.Config.Debug);
                            float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.InvisibleOnCommand, subClass, Time.time);
                            ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "invisible").Replace("{seconds}", timeLeft.ToString()));
                            return;
                        }

                        //scp268.Duration = subClass.FloatOptions.ContainsKey("InvisibleOnCommandDuration") ?
                        //    subClass.FloatOptions["InvisibleOnCommandDuration"]*2f : 30f*2f;

                        //ev.Player.ReferenceHub.playerEffectsController.EnableEffect(scp268);

                        ev.Player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
                        Tracking.PlayersInvisibleByCommand.Add(ev.Player);
                        Timing.CallDelayed(subClass.FloatOptions.ContainsKey("InvisibleOnCommandDuration") ?
                            subClass.FloatOptions["InvisibleOnCommandDuration"] : 30f, () => {
                                if (Tracking.PlayersInvisibleByCommand.Contains(ev.Player)) Tracking.PlayersInvisibleByCommand.Remove(ev.Player);
                                if (scp268.Enabled) ev.Player.ReferenceHub.playerEffectsController.DisableEffect<Scp268>();
                            });

                        Tracking.AddCooldown(ev.Player, AbilityType.InvisibleOnCommand);

                    }
                    break;
                default:
                    ev.IsAllowed = true;
                    break;
            }
        }

        public void AttemptRevive(SendingConsoleCommandEventArgs ev, SubClass subClass, bool necro = false)
        {
            Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} attempt", Subclass.Instance.Config.Debug);
            AbilityType ability = necro ? AbilityType.Necromancy : AbilityType.Revive;
            if (Tracking.OnCooldown(ev.Player, ability, subClass))
            {
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} on cooldown", Subclass.Instance.Config.Debug);
                float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, ability, subClass, Time.time);
                ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", necro ? "necromancy" : "revive").Replace("{seconds}", timeLeft.ToString()));
                return;
            }

            List<Collider> colliders = Physics.OverlapSphere(ev.Player.Position, 3f).Where(e => e.gameObject.GetComponentInParent<Ragdoll>() != null).ToList();

            colliders.Sort((Collider x, Collider y) =>
            {
                return Vector3.Distance(x.gameObject.transform.position, ev.Player.Position).CompareTo(Vector3.Distance(y.gameObject.transform.position, ev.Player.Position));
            });

            if (colliders.Count == 0)
            {
                ev.ReturnMessage = "You must be near a dead body to use this command";
                ev.Player.Broadcast(2, "You must be near a dead body.");
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} overlap did not hit a ragdoll", Subclass.Instance.Config.Debug);
                return;
            }

            Ragdoll doll = colliders[0].gameObject.GetComponentInParent<Ragdoll>();
            if (doll.owner == null)
            {
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                ev.ReturnMessage = "This player is not revivable.";
                ev.Player.Broadcast(2, "This player is not revivable.");
                return;
            }

            EPlayer owner = EPlayer.Get(colliders[0].gameObject.GetComponentInParent<Ragdoll>().owner.PlayerId);
            if (owner != null && !owner.IsAlive)
            {
                if (!necro && Tracking.GetPreviousTeam(owner) != null &&
                Tracking.GetPreviousTeam(owner) == ev.Player.Team) owner.Role = (RoleType)Tracking.GetPreviousRole(owner);
                else if (necro)
                {
                    owner.Role = RoleType.Scp0492;
                    Tracking.AddZombie(ev.Player, owner);
                    owner.IsFriendlyFireEnabled = true;
                }
                if (owner.Role != RoleType.Spectator)
                {
                    Timing.CallDelayed(0.2f, () =>
                    {
                        owner.Position = colliders[0].gameObject.transform.position;
                    });
                    UnityEngine.Object.DestroyImmediate(doll.gameObject, true);
                    Tracking.AddCooldown(ev.Player, ability);
                    Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} succeeded", Subclass.Instance.Config.Debug);
                }
                else
                {
                    Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                    ev.ReturnMessage = "This player is not revivable.";
                    ev.Player.Broadcast(2, "This player is not revivable.");
                }
            }
            else
            {
                Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} failed", Subclass.Instance.Config.Debug);
                ev.ReturnMessage = "This player is not revivable.";
                ev.Player.Broadcast(2, "This player is not revivable.");
            }
        }
    }
}
