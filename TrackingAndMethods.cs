using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Loader;
using Exiled.Permissions.Extensions;
using Interactables.Interobjects.DoorUtils;
using MEC;
using Mono.Unix.Native;
using Subclass.Effects;
using Subclass.MonoBehaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass
{
	public class TrackingAndMethods
	{
		public static Dictionary<RoleType, int> rolesForClass = new Dictionary<RoleType, int>();

		public static Dictionary<SubClass, int> SubClassesSpawned = new Dictionary<SubClass, int>();

		public static Dictionary<Player, SubClass> PlayersWithSubclasses = new Dictionary<Player, SubClass>();

		public static Dictionary<Player, Dictionary<AbilityType, float>> Cooldowns = new Dictionary<Player, Dictionary<AbilityType, float>>();
		public static Dictionary<Player, Dictionary<AbilityType, int>> AbilityUses = new Dictionary<Player, Dictionary<AbilityType, int>>();

		public static Dictionary<Player, float> PlayersThatBypassedTeslaGates = new Dictionary<Player, float>();

		public static Dictionary<Player, float> PlayersThatJustGotAClass = new Dictionary<Player, float>();

		public static Dictionary<Player, List<Player>> PlayersWithZombies = new Dictionary<Player, List<Player>>();
		public static Dictionary<Player, List<Player>> PlayersThatHadZombies = new Dictionary<Player, List<Player>>();

		public static Dictionary<Player, RoleType> PreviousRoles = new Dictionary<Player, RoleType>();
		public static Dictionary<Player, SubClass> PreviousSubclasses = new Dictionary<Player, SubClass>();
		public static Dictionary<uint, RoleType> RagdollRoles = new Dictionary<uint, RoleType>();
		// public static Player LastDiedTo035 = null; - I would love to implement this and keep 035 data... but there's no event to listen to for a player dying by picking up 035 :(

		public static List<Player> FriendlyFired = new List<Player>();

		public static List<Player> PlayersInvisibleByCommand = new List<Player>();
		public static List<Player> PlayersVenting = new List<Player>();

		public static List<string> QueuedCassieMessages = new List<string>();

		public static float RoundStartedAt = 0f;

		public static List<Player> NextSpawnWave = new List<Player>();
		public static Dictionary<RoleType, SubClass> NextSpawnWaveGetsRole = new Dictionary<RoleType, SubClass>();
		public static Dictionary<Team, int> NumSpawnWaves = new Dictionary<Team, int>();
		public static List<SubClass> SpawnWaveSpawns = new List<SubClass>();
		public static Dictionary<SubClass, int> ClassesGiven = new Dictionary<SubClass, int>();
		public static List<SubClass> DontGiveClasses = new List<SubClass>();

		public static Dictionary<Player, string> PreviousBadges = new Dictionary<Player, string>();

		static System.Random rnd = new System.Random();


		public static void MaybeAddRoles(Player player, bool is035 = false, bool escaped = false)
		{
			if (IsGhost(player)) return;
			if (!rolesForClass.ContainsKey(player.Role))
				rolesForClass.Add(player.Role, Subclass.Instance.Classes.Values.Count(e => e.BoolOptions["Enabled"] &&
					e.AffectsRoles.Contains(player.Role)));

			if (rolesForClass[player.Role] > 0)
			{
				List<string> teamsAlive = GetTeamsAlive();

				bool gotUniqueClass = CheckUserClass(player, is035, escaped, teamsAlive) || CheckPermissionClass(player, is035, escaped, teamsAlive);

				if (gotUniqueClass) return;

				if (!Subclass.Instance.Config.AdditiveChance)
				{
					Log.Debug($"Evaluating possible subclasses for player with name {player.Nickname}", Subclass.Instance.Config.Debug);
					foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] && e.AffectsRoles.Contains(player.Role) &&
					(!e.IntOptions.ContainsKey("MaxSpawnPerRound") || ClassesSpawned(e) < e.IntOptions["MaxSpawnPerRound"]) &&
					(!e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") || !e.BoolOptions["OnlyAffectsSpawnWave"]) &&
					(!e.BoolOptions.ContainsKey("GivenOnEscape") || ((!e.BoolOptions["GivenOnEscape"] && !escaped) || e.BoolOptions["GivenOnEscape"])) &&
					(!e.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.BoolOptions["WaitForSpawnWaves"] &&
					GetNumWavesSpawned(e.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
					(Team)Enum.Parse(typeof(Team), e.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.IntOptions["NumSpawnWavesToWait"])) &&
					EvaluateSpawnParameters(e)))
					{
						double rng = (rnd.NextDouble() * 100);
						Log.Debug($"Evaluating possible subclass {subClass.Name} for player with name {player.Nickname}. Number generated: {rng}, must be less than {subClass.FloatOptions["ChanceToGet"]} to get class", Subclass.Instance.Config.Debug);

						if (DontGiveClasses.Contains(subClass))
						{
							Log.Debug("Not giving subclass, MaxPerSpawnWave exceeded.", Subclass.Instance.Config.Debug);
							continue;
						}

						if (rng < subClass.FloatOptions["ChanceToGet"] &&
							(!subClass.IntOptions.ContainsKey("MaxAlive") ||
							PlayersWithSubclasses.Where(e => e.Value.Name == subClass.Name).Count() < subClass.IntOptions["MaxAlive"]) &&
							(subClass.EndsRoundWith == "RIP" || subClass.EndsRoundWith == "ALL" || teamsAlive.Contains(subClass.EndsRoundWith)))
						{
							Log.Debug($"{player.Nickname} attempting to be given subclass {subClass.Name}", Subclass.Instance.Config.Debug);
							AddClass(player, subClass, is035, is035 || escaped, escaped);
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
					double num = (rnd.NextDouble() * 100);
					Log.Debug($"Evaluating possible subclasses for player with name {player.Nickname}. Additive chance. Number generated: {num}", Subclass.Instance.Config.Debug);


					if (!Subclass.Instance.ClassesAdditive.ContainsKey(player.Role)) return;

					foreach (var possibity in Subclass.Instance.ClassesAdditive[player.Role].Where(e => e.Key.BoolOptions["Enabled"] &&
					e.Key.AffectsRoles.Contains(player.Role) && (!e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") || !e.Key.BoolOptions["OnlyAffectsSpawnWave"]) &&
					(!e.Key.BoolOptions.ContainsKey("GivenOnEscape") || ((!e.Key.BoolOptions["GivenOnEscape"] && !escaped) || e.Key.BoolOptions["GivenOnEscape"])) &&
					(!e.Key.IntOptions.ContainsKey("MaxSpawnPerRound") || ClassesSpawned(e.Key) < e.Key.IntOptions["MaxSpawnPerRound"]) &&
					(!e.Key.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.Key.BoolOptions["WaitForSpawnWaves"] &&
					GetNumWavesSpawned(e.Key.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
					(Team)Enum.Parse(typeof(Team), e.Key.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.Key.IntOptions["NumSpawnWavesToWait"])) &&
					EvaluateSpawnParameters(e.Key)))
					{
						Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for player with name {player.Nickname}. Num ({num}) must be less than {possibity.Value} to obtain.", Subclass.Instance.Config.Debug);
						if (num < possibity.Value && (!possibity.Key.IntOptions.ContainsKey("MaxAlive") ||
							PlayersWithSubclasses.Where(e => e.Value.Name == possibity.Key.Name).Count() < possibity.Key.IntOptions["MaxAlive"]) &&
							(possibity.Key.EndsRoundWith == "RIP" || possibity.Key.EndsRoundWith == "ALL" || teamsAlive.Contains(possibity.Key.EndsRoundWith)))
						{
							Log.Debug($"{player.Nickname} attempting to be given subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
							AddClass(player, possibity.Key, is035, is035 || escaped, escaped);
							break;
						}
						else
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

		public static bool CheckUserClass(Player player, bool is035, bool escaped, List<string> teamsAlive)
		{
			foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] && e.AffectsRoles.Contains(player.Role) &&
				e.AffectsUsers.ContainsKey(player.UserId) &&
				(!e.IntOptions.ContainsKey("MaxSpawnPerRound") || ClassesSpawned(e) < e.IntOptions["MaxSpawnPerRound"]) &&
				(!e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") || !e.BoolOptions["OnlyAffectsSpawnWave"]) &&
				(!e.BoolOptions.ContainsKey("OnlyGivenOnEscape") || (e.BoolOptions["OnlyGivenOnEscape"] && escaped)) &&
				(!e.BoolOptions.ContainsKey("GivenOnEscape") || ((!e.BoolOptions["GivenOnEscape"] && !escaped) || e.BoolOptions["GivenOnEscape"])) &&
				(!e.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.BoolOptions["WaitForSpawnWaves"] &&
				GetNumWavesSpawned(e.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
				(Team)Enum.Parse(typeof(Team), e.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.IntOptions["NumSpawnWavesToWait"])) &&
				EvaluateSpawnParameters(e)))
			{
				double rng = (rnd.NextDouble() * 100);
				Log.Debug($"Evaluating possible unique subclass {subClass.Name} for player with name {player.Nickname}. Number generated: {rng}, must be less than {subClass.AffectsUsers[player.UserId]} to get class", Subclass.Instance.Config.Debug);
				if (DontGiveClasses.Contains(subClass))
				{
					Log.Debug("Not giving subclass, MaxPerSpawnWave exceeded.", Subclass.Instance.Config.Debug);
					continue;
				}

				if (rng < subClass.AffectsUsers[player.UserId] && (!subClass.IntOptions.ContainsKey("MaxAlive") ||
					PlayersWithSubclasses.Where(e => e.Value.Name == subClass.Name).Count() < subClass.IntOptions["MaxAlive"]) &&
					(subClass.EndsRoundWith == "RIP" || subClass.EndsRoundWith == "ALL" || teamsAlive.Contains(subClass.EndsRoundWith)))
				{
					Log.Debug($"{player.Nickname} attempting to be given subclass {subClass.Name}", Subclass.Instance.Config.Debug);
					AddClass(player, subClass, is035, is035 || escaped, escaped);
					return true;
				}
			}
			return false;
		}

		public static bool CheckPermissionClass(Player player, bool is035, bool escaped, List<string> teamsAlive)
		{
			foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] && e.AffectsRoles.Contains(player.Role) &&
				e.Permissions.Count > 0 && e.Permissions.Keys.Any(p => player.CheckPermission("sc." + p)) &&
				(!e.IntOptions.ContainsKey("MaxSpawnPerRound") || ClassesSpawned(e) < e.IntOptions["MaxSpawnPerRound"]) &&
				(!e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") || !e.BoolOptions["OnlyAffectsSpawnWave"]) &&
				(!e.BoolOptions.ContainsKey("OnlyGivenOnEscape") || (e.BoolOptions["OnlyGivenOnEscape"] && escaped)) &&
				(!e.BoolOptions.ContainsKey("GivenOnEscape") || ((!e.BoolOptions["GivenOnEscape"] && !escaped) || e.BoolOptions["GivenOnEscape"])) &&
				(!e.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.BoolOptions["WaitForSpawnWaves"] &&
				GetNumWavesSpawned(e.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
				(Team)Enum.Parse(typeof(Team), e.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.IntOptions["NumSpawnWavesToWait"])) &&
				EvaluateSpawnParameters(e)))
			{
				double rng = (rnd.NextDouble() * 100);
				float needed = subClass.Permissions.First(p => player.CheckPermission("sc." + p.Key)).Value;
				Log.Debug($"Evaluating possible permission subclass {subClass.Name} for player with name {player.Nickname}. Number generated: {rng}, must be less than {needed} to get class", Subclass.Instance.Config.Debug);
				if (DontGiveClasses.Contains(subClass))
				{
					Log.Debug("Not giving subclass, MaxPerSpawnWave exceeded.", Subclass.Instance.Config.Debug);
					continue;
				}

				if (rng < needed && (!subClass.IntOptions.ContainsKey("MaxAlive") ||
					PlayersWithSubclasses.Where(e => e.Value.Name == subClass.Name).Count() < subClass.IntOptions["MaxAlive"]) &&
					(subClass.EndsRoundWith == "RIP" || subClass.EndsRoundWith == "ALL" || teamsAlive.Contains(subClass.EndsRoundWith)))
				{
					Log.Debug($"{player.Nickname} attempting to be given subclass {subClass.Name}", Subclass.Instance.Config.Debug);
					AddClass(player, subClass, is035, is035 || escaped, escaped);
					return true;
				}
			}
			return false;
		}

		public static void AddClass(Player player, SubClass subClass, bool is035 = false, bool lite = false, bool escaped = false, bool disguised = false)
		{
			if (player == null) return;
			if (is035)
			{
				SubClass copy = new SubClass(subClass);
				if (!copy.Abilities.Contains(AbilityType.Disable096Trigger)) copy.Abilities.Add(AbilityType.Disable096Trigger);
				if (!copy.Abilities.Contains(AbilityType.Disable173Stop)) copy.Abilities.Add(AbilityType.Disable173Stop);
				if (!copy.Abilities.Contains(AbilityType.NoSCPDamage)) copy.Abilities.Add(AbilityType.NoSCPDamage);
				copy.BoolOptions["HasFriendlyFire"] = true;
				copy.BoolOptions["TakesFriendlyFire"] = true;
				copy.SpawnsAs = RoleType.None;
				copy.SpawnLocations.Clear();
				copy.SpawnLocations.Add("Unknown");
				copy.IntOptions["MaxHealth"] = -1;
				copy.IntOptions["HealthOnSpawn"] = -1;
				copy.IntOptions["MaxArmor"] = -1;
				copy.IntOptions["ArmorOnSpawn"] = -1;
				copy.SpawnItems.Clear();
				copy.RolesThatCantDamage.Clear();
				copy.StringOptions["GotClassMessage"] = subClass.StringOptions["GotClassMessage"] + " You are SCP-035.";
				copy.CantDamageRoles.Clear();

				subClass = new SubClass(copy.Name + "-SCP-035 (p)", copy.AffectsRoles, copy.StringOptions, copy.BoolOptions, copy.IntOptions,
					copy.FloatOptions, copy.SpawnLocations, copy.SpawnItems,
					new Dictionary<AmmoType, int>()
					{
						{ AmmoType.Nato556, -1 },
						{ AmmoType.Nato762, -1 },
						{ AmmoType.Nato9, -1 }
					}, copy.Abilities, copy.AbilityCooldowns, copy.AdvancedFFRules, copy.OnHitEffects, copy.OnSpawnEffects,
					copy.RolesThatCantDamage, "SCP", RoleType.None, null, subClass.OnDamagedEffects, null
				);
			}
			if (NextSpawnWave.Contains(player) && NextSpawnWaveGetsRole.ContainsKey(player.Role) && !SpawnWaveSpawns.Contains(subClass))
			{
				if (SubClassesSpawned.ContainsKey(subClass)) SubClassesSpawned[subClass]++;
				else SubClassesSpawned.Add(subClass, 1);
				SpawnWaveSpawns.Add(subClass);
			}
			else if (!SpawnWaveSpawns.Contains(subClass))
			{
				if (SubClassesSpawned.ContainsKey(subClass)) SubClassesSpawned[subClass]++;
				else SubClassesSpawned.Add(subClass, 1);
			}
			if (!disguised) PlayersWithSubclasses.Add(player, subClass);
			if (!PlayersThatJustGotAClass.ContainsKey(player)) PlayersThatJustGotAClass.Add(player, Time.time + 3f);
			else PlayersThatJustGotAClass[player] = Time.time + 3f;

			int spawnIndex = rnd.Next(subClass.SpawnLocations.Count);
			List<Vector3> spawnLocations = new List<Vector3>();
			if (subClass.SpawnLocations.Contains("Lcz173Armory"))
			{
				DoorVariant door = GameObject.FindObjectsOfType<DoorVariant>().FirstOrDefault(dr => dr.name.ToUpper() == "173_ARMORY");
				spawnLocations.Add(door.transform.position + new Vector3(1f, 0, 1f));
			}
			
			if (subClass.SpawnLocations.Contains("Lcz173Connector"))
			{
				DoorVariant door = GameObject.FindObjectsOfType<DoorVariant>().FirstOrDefault(dr => dr.name.ToUpper() == "173_CONNECTOR");
				spawnLocations.Add(door.transform.position + new Vector3(1f, 0, 1f));
			}
			
			if (subClass.SpawnLocations.Contains("Lcz173") )
			{
				DoorVariant door = GameObject.FindObjectsOfType<DoorVariant>().FirstOrDefault(dr => dr.name.ToUpper() == "173_GATE");
				spawnLocations.Add(door.transform.position + new Vector3(1f, 0, 1f));
			}

			spawnLocations.AddRange(Map.Rooms.Where(r => subClass.SpawnLocations.Contains(r.Type.ToString())).Select(r => r.Transform.position));
			
			int tries = 0;
			while (!(subClass.SpawnLocations[spawnIndex] == "Unknown" || subClass.SpawnLocations[spawnIndex] == "Lcz173Armory" || subClass.SpawnLocations[spawnIndex] == "Lcz173" 
				|| subClass.SpawnLocations[spawnIndex] == "Lcz173Connector") && !Map.Rooms.Any(r => r.Type.ToString() == subClass.SpawnLocations[spawnIndex]))
			{
				spawnIndex = rnd.Next(subClass.SpawnLocations.Count);
				tries++;
				if (tries > subClass.SpawnLocations.Count)
				{
					spawnIndex = -1;
					break;
				}
			}

			try
			{
				player.Broadcast(subClass.FloatOptions.ContainsKey("BroadcastTimer") ? (ushort)subClass.FloatOptions["BroadcastTimer"] : (ushort)Subclass.Instance.Config.GlobalBroadcastTime, subClass.StringOptions["GotClassMessage"]);
				if (subClass.StringOptions.ContainsKey("CassieAnnouncement") &&
					!QueuedCassieMessages.Contains(subClass.StringOptions["CassieAnnouncement"])) QueuedCassieMessages.Add(subClass.StringOptions["CassieAnnouncement"]);

				if ((!lite || escaped) && subClass.SpawnsAs != RoleType.None)
				{
					player.SetRole(subClass.SpawnsAs, true);
				}

				if ((!lite || escaped) && subClass.SpawnItems.Count != 0)
				{
					player.Inventory.items.Clear();
					foreach (var item in subClass.SpawnItems)
					{
						foreach (var item2 in item.Value)
						{
							if ((rnd.NextDouble() * 100) < subClass.SpawnItems[item.Key][item2.Key])
							{
								if (item2.Key == ItemType.None) break;
								player.AddItem(item2.Key);
								break;
							}
						}
					}
				}
				if (subClass.IntOptions["MaxHealth"] != -1) player.MaxHealth = subClass.IntOptions["MaxHealth"];
				if ((!lite || escaped) && subClass.IntOptions["HealthOnSpawn"] != -1) player.Health = subClass.IntOptions["HealthOnSpawn"];
				if (subClass.IntOptions["MaxArmor"] != -1) player.MaxAdrenalineHealth = subClass.IntOptions["MaxArmor"];
				if ((!lite || escaped) && subClass.IntOptions["ArmorOnSpawn"] != -1) player.AdrenalineHealth = subClass.IntOptions["ArmorOnSpawn"];

				Timing.CallDelayed(0.3f, () =>
				{
					Vector3 scale = new Vector3(player.Scale.x, player.Scale.y, player.Scale.z);

					if (subClass.FloatOptions.ContainsKey("ScaleX")) scale.x = subClass.FloatOptions["ScaleX"];
					if (subClass.FloatOptions.ContainsKey("ScaleY")) scale.y = subClass.FloatOptions["ScaleY"];
					if (subClass.FloatOptions.ContainsKey("ScaleZ")) scale.z = subClass.FloatOptions["ScaleZ"];

					player.Scale = scale;
				});

				if (!subClass.BoolOptions["DisregardHasFF"])
				{
					player.IsFriendlyFireEnabled = subClass.BoolOptions["HasFriendlyFire"];
				}
			}
			catch (KeyNotFoundException e)
			{
				Log.Error($"A required option was not provided. Class: {subClass.Name}");
				throw new Exception($"A required option was not provided. Class: {subClass.Name}");
			}

			if (subClass.StringOptions.ContainsKey("Nickname")) player.DisplayNickname = subClass.StringOptions["Nickname"].Replace("{name}", player.Nickname);

			if (subClass.Abilities.Contains(AbilityType.GodMode)) player.IsGodModeEnabled = true;
			if (subClass.Abilities.Contains(AbilityType.InvisibleUntilInteract)) player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
			if (subClass.Abilities.Contains(AbilityType.InfiniteSprint)) player.GameObject.AddComponent<MonoBehaviours.InfiniteSprint>();
			if (subClass.Abilities.Contains(AbilityType.Disable173Stop)) Scp173.TurnedPlayers.Add(player);
			if (subClass.Abilities.Contains(AbilityType.Scp939Vision))
			{
				Timing.CallDelayed(0.3f, () =>
				{
					Visuals939 visuals = player.ReferenceHub.playerEffectsController.GetEffect<Visuals939>();
					visuals.Intensity = 3;
					player.ReferenceHub.playerEffectsController.EnableEffect(visuals);
				});
			}
			if (subClass.Abilities.Contains(AbilityType.NoArmorDecay)) player.ReferenceHub.playerStats.artificialHpDecay = 0f;

			if ((!lite || escaped) && subClass.SpawnAmmo[AmmoType.Nato556] != -1)
			{
				player.Ammo[(int)AmmoType.Nato556] = (uint)subClass.SpawnAmmo[AmmoType.Nato556];
			}

			if ((!lite || escaped) && subClass.SpawnAmmo[AmmoType.Nato762] != -1)
			{
				player.Ammo[(int)AmmoType.Nato762] = (uint)subClass.SpawnAmmo[AmmoType.Nato762];
			}

			if ((!lite || escaped) && subClass.SpawnAmmo[AmmoType.Nato9] != -1)
			{
				player.Ammo[(int)AmmoType.Nato9] = (uint)subClass.SpawnAmmo[AmmoType.Nato9];
			}

			if (subClass.Abilities.Contains(AbilityType.InfiniteAmmo))
			{
				player.Ammo[0] = uint.MaxValue;
				player.Ammo[1] = uint.MaxValue;
				player.Ammo[2] = uint.MaxValue;
			}

			if (subClass.Abilities.Contains(AbilityType.HealAura))
			{
				bool affectSelf = subClass.BoolOptions.ContainsKey("HealAuraAffectsSelf") ? subClass.BoolOptions["HealAuraAffectsSelf"] : true;
				bool affectAllies = subClass.BoolOptions.ContainsKey("HealAuraAffectsAllies") ? subClass.BoolOptions["HealAuraAffectsAllies"] : true;
				bool affectEnemies = subClass.BoolOptions.ContainsKey("HealAuraAffectsEnemies") ? subClass.BoolOptions["HealAuraAffectsEnemies"] : false;

				float healthPerTick = subClass.FloatOptions.ContainsKey("HealAuraHealthPerTick") ? subClass.FloatOptions["HealAuraHealthPerTick"] : 5f;
				float radius = subClass.FloatOptions.ContainsKey("HealAuraRadius") ? subClass.FloatOptions["HealAuraRadius"] : 4f;
				float tickRate = subClass.FloatOptions.ContainsKey("HealAuraTickRate") ? subClass.FloatOptions["HealAuraTickRate"] : 5f;

				player.ReferenceHub.playerEffectsController.AllEffects.Add(typeof(HealAura), new HealAura(player.ReferenceHub, healthPerTick, radius, affectSelf, affectAllies, affectEnemies, tickRate));
				Timing.CallDelayed(0.5f, () =>
				{
					player.ReferenceHub.playerEffectsController.EnableEffect<HealAura>(float.MaxValue);
				});
			}

			if (subClass.Abilities.Contains(AbilityType.DamageAura))
			{
				bool affectSelf = subClass.BoolOptions.ContainsKey("DamageAuraAffectsSelf") ? subClass.BoolOptions["DamageAuraAffectsSelf"] : false;
				bool affectAllies = subClass.BoolOptions.ContainsKey("DamageAuraAffectsAllies") ? subClass.BoolOptions["DamageAuraAffectsAllies"] : false;
				bool affectEnemies = subClass.BoolOptions.ContainsKey("DamageAuraAffectsEnemies") ? subClass.BoolOptions["DamageAuraAffectsEnemies"] : true;

				float healthPerTick = subClass.FloatOptions.ContainsKey("DamageAuraDamagePerTick") ? subClass.FloatOptions["DamageAuraDamagePerTick"] : 5f;
				float radius = subClass.FloatOptions.ContainsKey("DamageAuraRadius") ? subClass.FloatOptions["DamageAuraRadius"] : 4f;
				float tickRate = subClass.FloatOptions.ContainsKey("DamageAuraTickRate") ? subClass.FloatOptions["DamageAuraTickRate"] : 5f;

				player.ReferenceHub.playerEffectsController.AllEffects.Add(typeof(DamageAura), new DamageAura(player.ReferenceHub, healthPerTick, radius, affectSelf, affectAllies, affectEnemies, tickRate));
				Timing.CallDelayed(0.5f, () =>
				{
					player.ReferenceHub.playerEffectsController.EnableEffect<DamageAura>(float.MaxValue);
				});
			}

			if (subClass.Abilities.Contains(AbilityType.Regeneration))
			{
				float healthPerTick = subClass.FloatOptions.ContainsKey("RegenerationHealthPerTick") ? subClass.FloatOptions["RegenerationHealthPerTick"] : 2f;
				float tickRate = subClass.FloatOptions.ContainsKey("RegenerationTickRate") ? subClass.FloatOptions["RegenerationTickRate"] : 5f;

				player.ReferenceHub.playerEffectsController.AllEffects.Add(typeof(Regeneration), new Regeneration(player.ReferenceHub, healthPerTick, tickRate));
				Timing.CallDelayed(0.5f, () =>
				{
					player.ReferenceHub.playerEffectsController.EnableEffect<Regeneration>(float.MaxValue);
				});
			}

			if ((!lite || escaped))
			{
				foreach (var cooldown in subClass.InitialAbilityCooldowns)
				{
					AddCooldown(player, cooldown.Key, true);
				}
			}

			if (!is035)
			{
				if (player.GlobalBadge?.Type == 0) // Comply with verified server rules.
				{
					AddPreviousBadge(player, true);
					if (subClass.StringOptions.ContainsKey("Badge")) player.ReferenceHub.serverRoles.HiddenBadge = subClass.StringOptions["Badge"];
				}
				else
				{
					AddPreviousBadge(player);
					if (subClass.StringOptions.ContainsKey("Badge")) player.RankName = subClass.StringOptions["Badge"];
					if (subClass.StringOptions.ContainsKey("BadgeColor")) player.RankColor = subClass.StringOptions["BadgeColor"];
				}
			}

			if ((!lite || escaped) && subClass.OnSpawnEffects.Count != 0)
			{
				Timing.CallDelayed(0.1f, () =>
				{
					Log.Debug($"Subclass {subClass.Name} has on spawn effects", Subclass.Instance.Config.Debug);
					foreach (string effect in subClass.OnSpawnEffects)
					{
						Log.Debug($"Evaluating chance for on spawn {effect} for player {player.Nickname}", Subclass.Instance.Config.Debug);
						if (!subClass.FloatOptions.ContainsKey(("OnSpawn" + effect + "Chance")))
						{
							Log.Error($"ERROR! Spawn effect {effect} chance not found! Please make sure to add this to your float options");
							continue;
						}
						if ((rnd.NextDouble() * 100) < subClass.FloatOptions[("OnSpawn" + effect + "Chance")])
						{
							player.ReferenceHub.playerEffectsController.EnableByString(effect,
								subClass.FloatOptions.ContainsKey(("OnSpawn" + effect + "Duration")) ?
								subClass.FloatOptions[("OnSpawn" + effect + "Duration")] : -1, true);
							player.ReferenceHub.playerEffectsController.ChangeByString(effect, subClass.IntOptions.ContainsKey(("OnSpawn" + effect + "Intensity")) ?
							(byte)subClass.IntOptions[("OnSpawn" + effect + "Intensity")] : (byte)1);
							Log.Debug($"Player {player.Nickname} has been given effect {effect} on spawn", Subclass.Instance.Config.Debug);
						}
						else
						{
							Log.Debug($"Player {player.Nickname} has been not given effect {effect} on spawn", Subclass.Instance.Config.Debug);
						}
					}
				});
			}
			else
			{
				Log.Debug($"Subclass {subClass.Name} has no on spawn effects", Subclass.Instance.Config.Debug);
			}

			if (spawnIndex != -1 && (!lite || escaped) && subClass.SpawnLocations[spawnIndex] != "Unknown")
			{
				if (spawnLocations.Count != 0)
				{
						Timing.CallDelayed(0.3f, () =>
						{
							Vector3 offset = new Vector3(0, 1f, 0);
							if (subClass.FloatOptions.ContainsKey("SpawnOffsetX")) offset.x = subClass.FloatOptions["SpawnOffsetX"];
							if (subClass.FloatOptions.ContainsKey("SpawnOffsetY")) offset.y = subClass.FloatOptions["SpawnOffsetY"];
							if (subClass.FloatOptions.ContainsKey("SpawnOffsetZ")) offset.z = subClass.FloatOptions["SpawnOffsetZ"];
							Vector3 pos = spawnLocations[rnd.Next(spawnLocations.Count)] + offset;
							player.Position = pos;
						});
				}
			}
			else if (spawnIndex == -1)
				Log.Debug($"Unable to set spawn for class {subClass.Name} for player {player.Nickname}. No rooms found on map.", Subclass.Instance.Config.Debug);

			if (subClass.IntOptions.ContainsKey("MaxPerSpawnWave"))
			{
				if (!ClassesGiven.ContainsKey(subClass))
				{
					ClassesGiven.Add(subClass, 1);
					Timing.CallDelayed(5f, () =>
					{
						DontGiveClasses.Clear();
						ClassesGiven.Clear();
					});
				}
				else ClassesGiven[subClass]++;
				if (ClassesGiven[subClass] >= subClass.IntOptions["MaxPerSpawnWave"])
				{
					if (!DontGiveClasses.Contains(subClass))
					{
						DontGiveClasses.Add(subClass);
					}
				}
			}

			if (player.Role != RoleType.ClassD && player.Role != RoleType.Scientist && (subClass.EscapesAs[0] != RoleType.None || subClass.EscapesAs[1] != RoleType.None))
			{
				player.GameObject.AddComponent<EscapeBehaviour>();

				EscapeBehaviour eb = player.GameObject.GetComponent<EscapeBehaviour>();
				eb.EscapesAsNotCuffed = subClass.EscapesAs[0];
				eb.EscapesAsCuffed = subClass.EscapesAs[1];
			}
			Log.Debug($"Player with name {player.Nickname} got subclass {subClass.Name}", Subclass.Instance.Config.Debug);
		}

		public static void RemoveAndAddRoles(Player p, bool dontAddRoles = false, bool is035 = false, bool escaped = false, bool disguised = false)
		{
			if (PlayersThatJustGotAClass.ContainsKey(p) && PlayersThatJustGotAClass[p] > Time.time) return;
			if (RoundJustStarted()) return;
			if (!disguised)
			{
				if (PlayersInvisibleByCommand.Contains(p)) PlayersInvisibleByCommand.Remove(p);
				if (Cooldowns.ContainsKey(p)) Cooldowns.Remove(p);
				if (FriendlyFired.Contains(p)) FriendlyFired.RemoveAll(e => e == p);
				if (PlayersWithSubclasses.ContainsKey(p) && PlayersWithSubclasses[p].Abilities.Contains(AbilityType.Disable173Stop)
					&& Scp173.TurnedPlayers.Contains(p)) Scp173.TurnedPlayers.Remove(p);
				if (PlayersWithSubclasses.ContainsKey(p) && PlayersWithSubclasses[p].Abilities.Contains(AbilityType.NoArmorDecay))
					p.ReferenceHub.playerStats.artificialHpDecay = 0.75f;
				if (PlayersInvisibleByCommand.Contains(p)) PlayersInvisibleByCommand.Remove(p);
				if (PlayersVenting.Contains(p)) PlayersVenting.Remove(p);
			}

			//if (PlayersWithZombies.ContainsKey(p) && escaped)
			//{
			//    PlayersThatHadZombies.Add(p, PlayersWithZombies[p]);
			//    foreach (Player z in PlayersThatHadZombies[p])
			//    {
			//        z.GameObject.AddComponent<EscapeBehaviour>();

			//        RoleType r = RoleType.None;

			//        z.GameObject.GetComponent<EscapeBehaviour>().EscapesAs = r;
			//    }
			//    PlayersWithZombies.Remove(p);
			//}

			if (p.ReferenceHub.serverRoles.HiddenBadge != null && p.ReferenceHub.serverRoles.HiddenBadge != "") p.ReferenceHub.serverRoles.HiddenBadge = null;


			SubClass subClass = PlayersWithSubclasses.ContainsKey(p) ? PlayersWithSubclasses[p] : null;

			if (subClass != null)
			{
				if (!PreviousSubclasses.ContainsKey(p)) PreviousSubclasses.Add(p, subClass);
				else PreviousSubclasses[p] = subClass;

				if (PreviousBadges.ContainsKey(p))
				{
					if (subClass.StringOptions.ContainsKey("Badge") && p.RankName == subClass.StringOptions["Badge"])
					{
						p.RankName = PreviousBadges.ContainsKey(p) ? System.Text.RegularExpressions.Regex.Split(PreviousBadges[p], System.Text.RegularExpressions.Regex.Escape(" [-/-] "))[0] : null;
						p.RankColor = PreviousBadges.ContainsKey(p) ? System.Text.RegularExpressions.Regex.Split(PreviousBadges[p], System.Text.RegularExpressions.Regex.Escape(" [-/-] "))[1] : null;
					}
					else if (subClass.StringOptions.ContainsKey("Badge") && p.ReferenceHub.serverRoles.HiddenBadge == subClass.StringOptions["Badge"])
					{
						p.ReferenceHub.serverRoles.HiddenBadge = PreviousBadges.ContainsKey(p) ? System.Text.RegularExpressions.Regex.Split(PreviousBadges[p], System.Text.RegularExpressions.Regex.Escape(" [-/-] "))[0] : null;
					}
				}

				if (subClass.StringOptions.ContainsKey("Nickname")) p.DisplayNickname = null;

				if (subClass.Abilities.Contains(AbilityType.HealAura))
				{
					p.ReferenceHub.playerEffectsController.DisableEffect<HealAura>();
					p.ReferenceHub.playerEffectsController.AllEffects.Remove(typeof(HealAura));
				}

				if (subClass.Abilities.Contains(AbilityType.DamageAura))
				{
					p.ReferenceHub.playerEffectsController.DisableEffect<DamageAura>();
					p.ReferenceHub.playerEffectsController.AllEffects.Remove(typeof(DamageAura));
				}

				if (subClass.Abilities.Contains(AbilityType.Regeneration))
				{
					p.ReferenceHub.playerEffectsController.DisableEffect<Regeneration>();
					p.ReferenceHub.playerEffectsController.AllEffects.Remove(typeof(Regeneration));
				}
			}

			if (p.GameObject != null && p.GameObject.GetComponent<InfiniteSprint>() != null)
			{
				Log.Debug($"Player {p.Nickname} has infinite stamina, destroying", Subclass.Instance.Config.Debug);
				p.GameObject.GetComponent<InfiniteSprint>()?.Destroy();
				p.IsUsingStamina = true; // Have to set it to true for it to remove fully... for some reason?
			}

			if (p.GameObject != null && p.GameObject.GetComponent<EscapeBehaviour>() != null)
			{
				Log.Debug($"Player {p.Nickname} has escapebehaviour, destroying", Subclass.Instance.Config.Debug);
				p.GameObject.GetComponent<EscapeBehaviour>()?.Destroy();
			}

			if (PlayersWithSubclasses.ContainsKey(p) && !disguised) PlayersWithSubclasses.Remove(p);

			//foreach (var effect in p.ReferenceHub.playerEffectsController.AllEffects)
			//{
			//	if (effect.Key == typeof(Visuals939)) continue;
			//	byte prev = effect.Value.Intensity;
			//	effect.Value.Intensity = 0;
			//	effect.Value.Duration = 0f;
			//	effect.Value.ServerOnIntensityChange(prev, 0);
			//}
			//p.ReferenceHub.playerEffectsController.Resync();

			if (escaped)
			{
				if (!PlayersThatJustGotAClass.ContainsKey(p)) PlayersThatJustGotAClass.Add(p, Time.time + 3f);
				else PlayersThatJustGotAClass[p] = Time.time + 3f;
			}
			if (!dontAddRoles) MaybeAddRoles(p, is035, escaped);
		}

		public static void AddToFF(Player p)
		{
			if (!FriendlyFired.Contains(p)) FriendlyFired.Add(p);
		}

		public static void TryToRemoveFromFF(Player p)
		{
			if (FriendlyFired.Contains(p)) FriendlyFired.Remove(p);
		}

		public static void AddCooldown(Player p, AbilityType ability, bool initial = false)
		{
			try
			{
				SubClass subClass = PlayersWithSubclasses[p];
				if (!Cooldowns.ContainsKey(p)) Cooldowns.Add(p, new Dictionary<AbilityType, float>());
				Cooldowns[p][ability] = Time.time + (!initial ? subClass.AbilityCooldowns[ability] : subClass.InitialAbilityCooldowns[ability]);
			}
			catch (KeyNotFoundException e)
			{
				throw new Exception($"You are missing an ability cooldown that MUST have a cooldown. Make sure to add {ability} to your ability cooldowns.", e);
			}
		}

		public static void UseAbility(Player p, AbilityType ability, SubClass subClass)
		{
			if (!subClass.IntOptions.ContainsKey(ability.ToString() + "MaxUses")) return;
			if (!AbilityUses.ContainsKey(p)) AbilityUses.Add(p, new Dictionary<AbilityType, int>());
			if (!AbilityUses[p].ContainsKey(ability)) AbilityUses[p].Add(ability, 0);
			AbilityUses[p][ability]++;
		}

		public static bool CanUseAbility(Player p, AbilityType ability, SubClass subClass)
		{
			if (!AbilityUses.ContainsKey(p) || !AbilityUses[p].ContainsKey(ability) || !subClass.IntOptions.ContainsKey(ability.ToString() + "MaxUses") ||
				AbilityUses[p][ability] < subClass.IntOptions[(ability.ToString() + "MaxUses")]) return true;
			return false;
		}

		public static void DisplayCantUseAbility(Player p, AbilityType ability, SubClass subClass, string abilityName)
		{
			p.ClearBroadcasts();
			p.Broadcast(4, subClass.StringOptions["OutOfAbilityUses"].Replace("{ability}", abilityName));
		}

		public static bool OnCooldown(Player p, AbilityType ability, SubClass subClass)
		{
			return Cooldowns.ContainsKey(p) && Cooldowns[p].ContainsKey(ability)
				&& Time.time <= Cooldowns[p][ability];
		}

		public static float TimeLeftOnCooldown(Player p, AbilityType ability, SubClass subClass, float time)
		{
			if (Cooldowns.ContainsKey(p) && Cooldowns[p].ContainsKey(ability))
			{
				return subClass.AbilityCooldowns[ability] - (time - Cooldowns[p][ability]);
			}
			return 0;
		}

		public static void DisplayCooldown(Player p, AbilityType ability, SubClass subClass, string abilityName, float time)
		{
			float timeLeft = TimeLeftOnCooldown(p, ability, subClass, time);
			p.ClearBroadcasts();
			p.Broadcast((ushort)Mathf.Clamp(timeLeft - timeLeft / 4, 0.5f, 3), subClass.StringOptions["AbilityCooldownMessage"].Replace("{ability}", abilityName).Replace("{seconds}", timeLeft.ToString()));
		}

		public static bool PlayerJustBypassedTeslaGate(Player p)
		{
			return PlayersThatBypassedTeslaGates.ContainsKey(p) && Time.time - PlayersThatBypassedTeslaGates[p] < 3f;
		}

		public static bool RoundJustStarted()
		{
			return Time.time - RoundStartedAt < 5f;
		}

		public static void AddPreviousTeam(Player p)
		{
			if (PreviousRoles.ContainsKey(p)) PreviousRoles[p] = p.Role;
			else PreviousRoles.Add(p, p.Role);
		}

		public static Nullable<RoleType> GetPreviousRole(Player p)
		{
			if (PreviousRoles.ContainsKey(p)) return PreviousRoles[p];
			return null;
		}

		public static Nullable<Team> GetPreviousTeam(Player p)
		{
			if (PreviousRoles.ContainsKey(p)) return PreviousRoles[p].GetTeam();
			return null;
		}

		public static void AddZombie(Player p, Player z)
		{
			if (!PlayersWithZombies.ContainsKey(p)) PlayersWithZombies.Add(p, new List<Player>());
			PlayersWithZombies[p].Add(z);
		}

		public static void RemoveZombie(Player p)
		{
			List<Player> toRemoveWith = new List<Player>();
			List<Player> toRemoveHad = new List<Player>();
			foreach (var item in PlayersWithZombies)
			{
				if (item.Value.Contains(p)) item.Value.Remove(p);
				if (item.Value.Count == 0) toRemoveWith.Add(item.Key);
			}
			foreach (var item in PlayersThatHadZombies)
			{
				if (item.Value.Contains(p)) item.Value.Remove(p);
				if (item.Value.Count == 0) toRemoveHad.Add(item.Key);
			}

			foreach (Player p1 in toRemoveWith) PlayersWithZombies.Remove(p1);
			foreach (Player p1 in toRemoveHad) PlayersThatHadZombies.Remove(p1);
		}

		public static bool PlayerHasFFToPlayer(Player attacker, Player target)
		{
			Log.Debug($"Checking FF rules for Attacker: {attacker.Nickname} Target: {target?.Nickname}", Subclass.Instance.Config.Debug);
			if (target != null)
			{
				Log.Debug($"Checking zombies", Subclass.Instance.Config.Debug);
				if (PlayersWithZombies.Where(p => p.Value.Contains(target)).Count() > 0)
				{
					return true;
				}

				Log.Debug($"Checking classes", Subclass.Instance.Config.Debug);
				if (PlayersWithSubclasses.ContainsKey(attacker) && PlayersWithSubclasses.ContainsKey(target) &&
					PlayersWithSubclasses[attacker].AdvancedFFRules.Contains(PlayersWithSubclasses[target].Name))
				{
					return true;
				}

				Log.Debug($"Checking FF rules in classes", Subclass.Instance.Config.Debug);
				if (FriendlyFired.Contains(target) ||
					(PlayersWithSubclasses.ContainsKey(attacker) &&
					!PlayersWithSubclasses[attacker].BoolOptions["DisregardHasFF"] && PlayersWithSubclasses[attacker].BoolOptions["HasFriendlyFire"]) ||
					(PlayersWithSubclasses.ContainsKey(target) && !PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
					PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]))
				{
					if (!FriendlyFired.Contains(target) && !(PlayersWithSubclasses.ContainsKey(target) && PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"]))
						AddToFF(attacker);
					return true;
				}
				else
				{
					Log.Debug($"Checking takes friendly fire", Subclass.Instance.Config.Debug);
					if (PlayersWithSubclasses.ContainsKey(target) && !PlayersWithSubclasses[target].BoolOptions["DisregardTakesFF"] &&
					!PlayersWithSubclasses[target].BoolOptions["TakesFriendlyFire"])
					{
						return false;
					}
				}
			}
			return false;
		}

		public static bool AllowedToDamage(Player t, Player a)
		{
			Log.Debug($"Checking allowed damage rules for Attacker: {a.Nickname} to target role: {t.Role}", Subclass.Instance.Config.Debug);
			if (a.Id == t.Id) return true;
			if (PlayersWithSubclasses.ContainsKey(a)) return !PlayersWithSubclasses[a].CantDamageRoles.Contains(t.Role);
			if (PlayersWithSubclasses.ContainsKey(t)) return !PlayersWithSubclasses[t].RolesThatCantDamage.Contains(a.Role);
			return true;
		}

		public static IEnumerator<float> CheckRoundEnd()
		{
			if (!Round.IsStarted || RoundJustStarted() || (Player.List.Count() == 1 && !GameCore.ConfigFile.ServerConfig.GetBool("end_round_on_one_player"))) yield break;
			Log.Debug("Checking round end", Subclass.Instance.Config.Debug);
			List<string> teamsAlive = GetTeamsAlive();

			List<string> uniqueTeamsAlive = new List<string>();

			foreach (string t in teamsAlive)
			{
				if (!uniqueTeamsAlive.Contains(t)) uniqueTeamsAlive.Add(t);
			}

			Log.Debug($"Number of unique teams alive: {uniqueTeamsAlive.Count}. Contains ALL? {uniqueTeamsAlive.Contains("ALL")}", Subclass.Instance.Config.Debug);
			if (Round.IsLocked) goto swap_classes;

			RoundSummary.SumInfo_ClassList classList = default;
			foreach (GameObject player in PlayerManager.players)
			{
				if (player != null)
				{
					CharacterClassManager component = player.GetComponent<CharacterClassManager>();
					if (component != null && component.Classes.CheckBounds(component.CurClass))
					{
						switch (component.Classes.SafeGet(component.CurClass).team)
						{
							case Team.SCP:
								if (component.CurClass == RoleType.Scp0492)
								{
									classList.zombies++;
								}
								else
								{
									classList.scps_except_zombies++;
								}
								continue;
							case Team.MTF:
								classList.mtf_and_guards++;
								continue;
							case Team.CHI:
								classList.chaos_insurgents++;
								continue;
							case Team.RSC:
								classList.scientists++;
								continue;
							case Team.CDP:
								classList.class_ds++;
								continue;
							default:
								continue;
						}
					}
				}
			}

			classList.warhead_kills = AlphaWarheadController.Host.detonated ? AlphaWarheadController.Host.warheadKills : -1;
			classList.time = (int)Time.realtimeSinceStartup;

			RoundSummary.LeadingTeam leadingTeam = RoundSummary.LeadingTeam.Draw;

			RoundSummary.roundTime = classList.time - RoundSummary.singleton.classlistStart.time;

			if (uniqueTeamsAlive.Count == 2 && uniqueTeamsAlive.Contains("SCP"))
			{
				List<Player> zombies = API.RevivedZombies();
				if (Player.List.Where(p => p.Team == Team.SCP).All(p => zombies.Contains(p)))
				{
					foreach (Player zombie in zombies) zombie.Kill();
				}
			}

			if (uniqueTeamsAlive.Count == 2 && uniqueTeamsAlive.Contains("ALL"))
			{
				string team = uniqueTeamsAlive.Find(t => t != "ALL");
				switch (team)
				{
					case "MTF":
						leadingTeam = RoundSummary.LeadingTeam.FacilityForces;
						break;
					case "CHI":
						leadingTeam = RoundSummary.LeadingTeam.ChaosInsurgency;
						break;
					case "SCP":
						leadingTeam = RoundSummary.LeadingTeam.Anomalies;
						break;
				}
				RoundSummary.singleton._roundEnded = true;
				RoundSummary.singleton.RpcShowRoundSummary(RoundSummary.singleton.classlistStart, classList, leadingTeam, RoundSummary.escaped_ds, RoundSummary.escaped_scientists, RoundSummary.kills_by_scp, Mathf.Clamp(GameCore.ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000));
				for (int i = 0; i < 50 * (Mathf.Clamp(GameCore.ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000) - 1); i++)
					yield return 0.0f;
				RoundSummary.singleton.RpcDimScreen();
				for (int i = 0; i < 50; i++)
					yield return 0.0f;
				PlayerManager.localPlayer.GetComponent<PlayerStats>().Roundrestart();
				yield break;
			}

			if (uniqueTeamsAlive.Count == 1)
			{
				if (PlayersWithSubclasses.Count > 0)
				{
					switch (PlayersWithSubclasses.First().Value.EndsRoundWith)
					{
						case "MTF":
							leadingTeam = RoundSummary.LeadingTeam.FacilityForces;
							break;
						case "CHI":
							leadingTeam = RoundSummary.LeadingTeam.ChaosInsurgency;
							break;
						case "SCP":
							leadingTeam = RoundSummary.LeadingTeam.Anomalies;
							break;
					}
				}
				RoundSummary.singleton._roundEnded = true;
				RoundSummary.singleton.RpcShowRoundSummary(RoundSummary.singleton.classlistStart, classList, leadingTeam, RoundSummary.escaped_ds, RoundSummary.escaped_scientists, RoundSummary.kills_by_scp, Mathf.Clamp(GameCore.ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000));
				for (int i = 0; i < 50 * (Mathf.Clamp(GameCore.ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000) - 1); i++)
					yield return 0.0f;
				RoundSummary.singleton.RpcDimScreen();
				for (int i = 0; i < 50; i++)
					yield return 0.0f;
				PlayerManager.localPlayer.GetComponent<PlayerStats>().Roundrestart();
				yield break;
			}


		swap_classes:
			if (PlayersWithSubclasses != null && PlayersWithSubclasses.Count(s => s.Value.EndsRoundWith != "RIP") > 0)
			{
				foreach (Player player in PlayersWithSubclasses.Keys)
				{
					if ((PlayersWithSubclasses[player].BoolOptions.ContainsKey("ActAsSpy") && PlayersWithSubclasses[player].BoolOptions["ActAsSpy"]) &&
						PlayersWithSubclasses[player].EndsRoundWith != "RIP" &&
						PlayersWithSubclasses[player].EndsRoundWith != "ALL" &&
						PlayersWithSubclasses[player].EndsRoundWith != player.Team.ToString() &&
						teamsAlive.Count(e => e == PlayersWithSubclasses[player].EndsRoundWith) == 1)
					{
						PlayersThatJustGotAClass[player] = Time.time + 3f;
						if (PlayersWithSubclasses[player].EndsRoundWith == "MTF") player.SetRole(RoleType.NtfScientist, true);
						else if (PlayersWithSubclasses[player].EndsRoundWith == "CHI") player.SetRole(RoleType.ChaosInsurgency, true);
						else player.SetRole(RoleType.Scp0492, true);
					}
				}
			}
		}

		public static int ClassesSpawned(SubClass subClass)
		{
			if (!SubClassesSpawned.ContainsKey(subClass)) return 0;
			return SubClassesSpawned[subClass];
		}

		public static void AddPreviousBadge(Player p, bool hidden = false)
		{
			if (hidden)
			{
				if (PreviousBadges.ContainsKey(p)) PreviousBadges[p] = p.ReferenceHub.serverRoles.HiddenBadge + " [-/-] ";
				else PreviousBadges.Add(p, p.ReferenceHub.serverRoles.HiddenBadge + " [-/-] ");
			}
			else
			{
				if (PreviousBadges.ContainsKey(p)) PreviousBadges[p] = p.RankName + " [-/-] " + p.RankColor;
				else PreviousBadges.Add(p, p.RankName + " [-/-] " + p.RankColor);
			}
		}

		public static int GetNumWavesSpawned(Team t)
		{
			if (t == Team.RIP)
			{
				int count = 0;
				foreach (var spawns in NumSpawnWaves)
				{
					count += spawns.Value;
				}
				return count;
			}
			else
			{
				return NumSpawnWaves.ContainsKey(t) ? NumSpawnWaves[t] : 0;
			}
		}

		public static List<string> GetTeamsAlive()
		{
			List<string> teamsAlive = Player.List.Select(p1 => p1.Team.ToString()).ToList();
			teamsAlive.RemoveAll(t => t == "RIP");
			foreach (var item in PlayersWithSubclasses.Where(s => s.Value.EndsRoundWith != "RIP"))
			{
				teamsAlive.Remove(item.Key.Team.ToString());
				teamsAlive.Add(item.Value.EndsRoundWith);
			}

			for (int i = 0; i < teamsAlive.Count; i++)
			{
				string t = teamsAlive[i];
				if (t == "CDP")
				{
					teamsAlive.RemoveAt(i);
					teamsAlive.Insert(i, "CHI");
				}
				else if (t == "RSC")
				{
					teamsAlive.RemoveAt(i);
					teamsAlive.Insert(i, "MTF");
				}
				else if (t == "TUT")
				{
					teamsAlive.RemoveAt(i);
					teamsAlive.Insert(i, "SCP");
				}
			}
			return teamsAlive;
		}

		public static bool EvaluateSpawnParameters(SubClass subClass)
		{
			List<string> evaluated = new List<string>();
			string seperator = Subclass.Instance.Config.SpawnParameterSeparator;
			foreach (var param in subClass.SpawnParameters)
			{
				if (evaluated.Contains(param.Key)) continue;
				evaluated.Add(param.Key);
				string[] args = param.Key.Split(new string[] { seperator }, StringSplitOptions.RemoveEmptyEntries);
				if (args[0] == "sc")
				{
					if (args[2] == "RangeMax" || args[2] == "RangeMin")
					{
						if (!IsInRange(evaluated, args, subClass, seperator)) 
						{ 
							Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
							return false; 
						}
						Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
					}
					else if (args[2] == "Alive")
					{
						if (PlayersWithSubclasses.Count(t => t.Value.Name == args[1]) != param.Value) 
						{ 
							Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug); 
							return false; 
						}
						Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
					}
				}
				else if (args[0] == "team")
				{
					try
					{
						Team team = (Team)Enum.Parse(typeof(Team), args[1]);
						if (args[2] == "RangeMax" || args[2] == "RangeMin")
						{
							if (!IsInRange(evaluated, args, subClass, seperator, team))
							{
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
								return false;
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
						else if (args[2] == "Alive")
						{
							if (GetTeamsAlive().Count(t => t == team.ToString()) != param.Value)
							{
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
								return false;
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
					}
					catch (ArgumentException e)
					{
						Log.Error($"Spawn parameters for class {subClass.Name} has an incorrect team name. Key: {param.Key}. {e}");
						return false;
					}
				}
				else if (args[0] == "players")
				{
					if (args[1] == "Alive")
					{
						if (args.Length == 3)
						{
							if (!IsInRange(evaluated, args, subClass, seperator)) 
							{ 
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug); 
								return false; 
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
						else
						{
							if (Player.List.Count(p => p.IsAlive) != param.Value) 
							{ 
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug); 
								return false; 
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
					}
					else if (args[1] == "Dead")
					{
						if (args.Length == 3)
						{
							if (!IsInRange(evaluated, args, subClass, seperator)) 
							{ 
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug); 
								return false; 
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
						else
						{
							if (Player.List.Count(p => !p.IsAlive) != param.Value) 
							{ 
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug); 
								return false; 
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
					}
				}
				else if (args[0] == "role")
				{
					try
					{
						RoleType role = (RoleType)Enum.Parse(typeof(RoleType), args[1]);
						if (args[2] == "RangeMax" || args[2] == "RangeMin")
						{
							if (!IsInRange(evaluated, args, subClass, seperator, Team.RIP, role)) 
							{ 
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
								return false; 
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
						else if (args[2] == "Alive")
						{
							if (Player.List.Count(p => p.Role == role) != param.Value) 
							{ 
								Log.Debug($"Did not pass spawn parameter: {param.Key}", Subclass.Instance.Config.Debug); 
								return false; 
							}
							Log.Debug($"Passed spawn parameter: {param.Key}", Subclass.Instance.Config.Debug);
						}
					}
					catch (ArgumentException e)
					{
						Log.Error($"Spawn parameters for class {subClass.Name} has an incorrect role name. Key: {param.Key}. {e}");
						return false;
					}
				}
			}
			return true;
		}

		public static bool IsInRange(List<string> evaluated, string[] args, SubClass subClass, string seperator, Team team = Team.RIP, RoleType role = RoleType.None)
		{
			int count = 0;
			if (args[0] == "sc") count = PlayersWithSubclasses.Count(e => e.Value.Name == args[1]);
			else if (args[0] == "team") count = GetTeamsAlive().Count(t => t == team.ToString());
			else if (args[0] == "players")
			{
				if (args[1] == "Alive") count = Player.List.Count(p => p.IsAlive);
				else if (args[1] == "Dead") count = Player.List.Count(p => !p.IsAlive);
			}
			else if (args[0] == "role") count = Player.List.Count(p => p.Role == role);
			string maxKey = $"{args[0]}{seperator}{args[1]}{seperator}RangeMax";
			string minKey = $"{args[0]}{seperator}{args[1]}{seperator}RangeMin";
			if (!subClass.SpawnParameters.ContainsKey(maxKey) || !subClass.SpawnParameters.ContainsKey(minKey))
			{
				Log.Error($"Subclass spawn parameters missing range key. Contains max key ({maxKey}): {subClass.SpawnParameters.ContainsKey(maxKey)}. Contains min key ({minKey}): {subClass.SpawnParameters.ContainsKey(minKey)}");
				return false;
			}
			if (args[2] == "RangeMax") evaluated.Add(minKey);
			else evaluated.Add(maxKey);
			int max = subClass.SpawnParameters[maxKey];
			int min = subClass.SpawnParameters[minKey];
			if (count < min || count > max) return false;
			return true;
		}

		public static RoleType? RagdollRole(Ragdoll doll)
		{
			if (!RagdollRoles.ContainsKey(doll.netId)) return null;
			return RagdollRoles[doll.netId];
		}

		public static Team RoleToTeam(RoleType role)
		{
			if (role == RoleType.ClassD) return Team.CHI;
			else if (role == RoleType.Tutorial) return Team.SCP;
			else if (role == RoleType.Scientist) return Team.MTF;
			else return role.GetTeam();
		}

		public static bool IsGhost(Player player)
		{
			Assembly assembly = Loader.Plugins.FirstOrDefault(pl => pl.Name == "GhostSpectator")?.Assembly;
			if (assembly == null) return false;
			return ((bool)assembly.GetType("GhostSpectator.API")?.GetMethod("IsGhost")?.Invoke(null, new object[] { player })) == true;
		}
	}
}
