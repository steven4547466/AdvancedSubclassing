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

namespace Subclass.Handlers
{
    public class Server
    {
        public void OnRoundStarted()
        {
            Timing.CallDelayed(0.1f, () =>
            {
                Log.Debug("Round started", Subclass.Instance.Config.Debug);
                Dictionary<RoleType, int> rolesForClass = new Dictionary<RoleType, int>();
                Random rnd = new Random();
                foreach (Exiled.API.Features.Player player in Exiled.API.Features.Player.List)
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
                                if (subClass.SpawnItems.Count != 1 || (subClass.SpawnItems.Count == 1 && subClass.SpawnItems[0] != ItemType.None)) {
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
                                if (subClass.Abilities.Contains(AbilityType.GodMode)) player.IsGodModeEnabled = true;
                                int index = rnd.Next(subClass.SpawnLocations.Count);
                                if (subClass.SpawnLocations[index] != RoomType.Unknown)
                                {
                                    List<Room> spawnLocations = Exiled.API.Features.Map.Rooms.Where(r => r.Type == subClass.SpawnLocations[index]).ToList();
                                    if (spawnLocations.Count != 0)
                                    {
                                        player.Position = spawnLocations[rnd.Next(spawnLocations.Count)].Position + new UnityEngine.Vector3(0, 1f, 0);
                                    }
                                }
                                if (subClass.Abilities.Contains(AbilityType.InvisibleUntilInteract)) player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
                                
                                if (subClass.SpawnAmmo[AmmoType.Nato556] != -1)
                                {
                                    player.Ammo[(int) AmmoType.Nato556] = (uint) subClass.SpawnAmmo[AmmoType.Nato556];
                                }

                                if (subClass.SpawnAmmo[AmmoType.Nato762] != -1)
                                {
                                    player.Ammo[(int) AmmoType.Nato762] = (uint) subClass.SpawnAmmo[AmmoType.Nato762];
                                }

                                if (subClass.SpawnAmmo[AmmoType.Nato9] != -1)
                                {
                                    player.Ammo[(int) AmmoType.Nato9] = (uint) subClass.SpawnAmmo[AmmoType.Nato9];
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
            });
        }

        public void OnRoundEnded(RoundEndedEventArgs ev)
        {
            Tracking.PlayersWithSubclasses.Clear();
            Tracking.Cooldowns.Clear();
            Tracking.FriendlyFired.Clear();
        }
    }
}
