using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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
            });
        }

        public void OnRoundEnded(RoundEndedEventArgs ev)
        {
            Tracking.PlayersWithSubclasses.Clear();
            Tracking.Cooldowns.Clear();
            Tracking.FriendlyFired.Clear();
            Tracking.PlayersThatBypassedTeslaGates.Clear();
            Tracking.PreviousRoles.Clear();
        }



        public void MaybeAddRoles(Exiled.API.Features.Player player)
        {
            if (!rolesForClass.ContainsKey(player.Role)) rolesForClass.Add(player.Role, Subclass.Instance.Classes.Values.Count(e => e.AffectsRole == player.Role));
            if (rolesForClass[player.Role] > 0)
            {
                Log.Debug($"Evaluating possible subclasses for player with name {player.Nickname}", Subclass.Instance.Config.Debug);
                foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] && e.AffectsRole == player.Role))
                {
                    Log.Debug($"Evaluating possible subclass {subClass.Name} for player with name {player.Nickname}", Subclass.Instance.Config.Debug);
                    if ((rnd.NextDouble() * 100) < subClass.FloatOptions["ChanceToGet"])
                    {
                        Log.Debug($"{player.Nickname} attempting to be given subclass {subClass.Name}", Subclass.Instance.Config.Debug);
                        Tracking.PlayersWithSubclasses.Add(player, subClass);
                        player.Broadcast(5, subClass.StringOptions["GotClassMessage"]);
                        if (subClass.SpawnItems.Count != 1 || (subClass.SpawnItems.Count == 1 && subClass.SpawnItems[0] != ItemType.None))
                        {
                            player.ClearInventory();
                            foreach (ItemType item in subClass.SpawnItems)
                            {
                                player.AddItem(item);
                            }
                        }
                        if (subClass.IntOptions["MaxHealth"] != -1) player.MaxHealth = subClass.IntOptions["MaxHealth"];
                        if (subClass.IntOptions["HealthOnSpawn"] != -1) player.Health = subClass.IntOptions["HealthOnSpawn"];
                        if (subClass.IntOptions["MaxArmor"] != -1) player.MaxAdrenalineHealth = subClass.IntOptions["MaxArmor"];
                        if (subClass.IntOptions["ArmorOnSpawn"] != -1) player.AdrenalineHealth = subClass.IntOptions["ArmorOnSpawn"];
                        if (!subClass.BoolOptions["DisregardHasFF"])
                        {
                            player.IsFriendlyFireEnabled = subClass.BoolOptions["HasFriendlyFire"];
                        }

                        int index = rnd.Next(subClass.SpawnLocations.Count);
                        if (subClass.SpawnLocations[index] != RoomType.Unknown)
                        {
                            List<Room> spawnLocations = Exiled.API.Features.Map.Rooms.Where(r => r.Type == subClass.SpawnLocations[index]).ToList();
                            if (spawnLocations.Count != 0)
                            {
                                player.Position = spawnLocations[rnd.Next(spawnLocations.Count)].Position + new UnityEngine.Vector3(0, 1f, 0);
                            }
                        }

                        if (subClass.Abilities.Contains(AbilityType.GodMode)) player.IsGodModeEnabled = true;
                        if (subClass.Abilities.Contains(AbilityType.InvisibleUntilInteract)) player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
                        if (subClass.Abilities.Contains(AbilityType.InfiniteSprint)) player.GameObject.AddComponent<MonoBehaviours.InfiniteSprint>();
                        if (subClass.Abilities.Contains(AbilityType.Disable096Trigger)) Scp096.TurnedPlayers.Add(player);
                        if (subClass.Abilities.Contains(AbilityType.Disable173Stop)) Scp173.TurnedPlayers.Add(player);
                        if (subClass.Abilities.Contains(AbilityType.Scp939Vision))
                        {
                            Visuals939 visuals = player.ReferenceHub.playerEffectsController.GetEffect<Visuals939>();
                            visuals.Intensity = 2;
                            player.ReferenceHub.playerEffectsController.EnableEffect(visuals);
                        }

                        if (subClass.SpawnAmmo[AmmoType.Nato556] != -1)
                        {
                            player.Ammo[(int)AmmoType.Nato556] = (uint)subClass.SpawnAmmo[AmmoType.Nato556];
                        }

                        if (subClass.SpawnAmmo[AmmoType.Nato762] != -1)
                        {
                            player.Ammo[(int)AmmoType.Nato762] = (uint)subClass.SpawnAmmo[AmmoType.Nato762];
                        }

                        if (subClass.SpawnAmmo[AmmoType.Nato9] != -1)
                        {
                            player.Ammo[(int)AmmoType.Nato9] = (uint)subClass.SpawnAmmo[AmmoType.Nato9];
                        }

                        if (player.RankName == null) // Comply with verified server rules.
                        {
                            if (subClass.StringOptions.ContainsKey("Badge")) player.RankName = subClass.StringOptions["Badge"];
                            if (subClass.StringOptions.ContainsKey("BadgeColor")) player.RankColor = subClass.StringOptions["BadgeColor"];
                        }

                        Log.Debug($"Player with name {player.Nickname} got subclass {subClass.Name}", Subclass.Instance.Config.Debug);
                        break;
                    }
                    else
                    {
                        Log.Debug($"Player with name {player.Nickname} did not get subclass {subClass.Name}", Subclass.Instance.Config.Debug);
                    }
                }
            }
            else
            {
                Log.Debug($"No subclasses for {player.Role}", Subclass.Instance.Config.Debug);
            }
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
                        if (Tracking.OnCooldown(ev.Player, AbilityType.Revive, subClass))
                        {
                            float timeLeft = Tracking.TimeLeftOnCooldown(ev.Player, AbilityType.Revive, subClass, Time.time);
                            ev.Player.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", "revive").Replace("{seconds}", timeLeft.ToString()));
                            return;
                        }
                        RaycastHit hit;
                        if (Physics.Raycast(ev.Player.CameraTransform.position, ev.Player.CameraTransform.forward, out hit, 3f))
                        {
                            Log.Debug($"Player {ev.Player.Nickname} revive raycast hit", Subclass.Instance.Config.Debug);
                            if (hit.collider.gameObject.GetComponentInParent<Ragdoll>() != null)
                            {
                                EPlayer owner = EPlayer.Get(hit.collider.gameObject.GetComponentInParent<Ragdoll>().owner.PlayerId);
                                if (owner != null && owner.Role == RoleType.Spectator && Tracking.GetPreviousTeam(owner) != null &&
                                    Tracking.GetPreviousTeam(owner) == ev.Player.Team)
                                {
                                    Ragdoll doll = hit.collider.gameObject.GetComponent<Ragdoll>();
                                    owner.Role = (RoleType)Tracking.GetPreviousRole(owner);
                                    owner.Position = doll.LastRagdollPos[doll.LastRagdollPos.Count - 1].position;
                                    UnityEngine.Object.DestroyImmediate(doll.gameObject, true);
                                    Tracking.AddCooldown(ev.Player, AbilityType.Revive);
                                    Log.Debug($"Player {ev.Player.Nickname} revive succeeded", Subclass.Instance.Config.Debug);
                                }
                                else
                                {
                                    Log.Debug($"Player {ev.Player.Nickname} revive failed", Subclass.Instance.Config.Debug);
                                    ev.ReturnMessage = "This player is not revivable.";
                                    ev.Player.Broadcast(2, "This player is not revivable.");
                                }
                            }
                            else {
                                ev.ReturnMessage = "You must be looking at a dead body to use this command";
                                ev.Player.Broadcast(2, "You must be looking at a dead body.");
                                Log.Debug($"Player {ev.Player.Nickname} revive raycast did not hit a ragdoll, hit {hit.collider.gameObject.name}", Subclass.Instance.Config.Debug);
                            }
                        }
                        else {
                            ev.ReturnMessage = "You must be looking at a dead body to use this command";
                            ev.Player.Broadcast(2, "You must be looking at a dead body.");
                            Log.Debug($"Player {ev.Player.Nickname} revive raycast did not hit anything", Subclass.Instance.Config.Debug);
                        }
                    }else
                    {
                        ev.ReturnMessage = "You don't have the ability to revive!";
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
                            EPlayer.Get(PlayerCollider.gameObject).ReferenceHub.footstepSync.CallCmdScp939Noise(5f);
                        }

                        Log.Debug($"Player {ev.Player.Nickname} successfully used echolocate", Subclass.Instance.Config.Debug);
                    } 
                    break;
                default:
                    ev.IsAllowed = true;
                    break;
            }
        }
    }
}
