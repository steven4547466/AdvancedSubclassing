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
			TrackingAndMethods.RoundStartedAt = Time.time;
			Timing.CallDelayed(Subclass.Instance.CommonUtilsEnabled ? 2f : 0.1f, () =>
			{
				Log.Debug("Round started!", Subclass.Instance.Config.Debug);
				foreach (EPlayer player in EPlayer.List)
				{
					TrackingAndMethods.MaybeAddRoles(player);
				}
				foreach (string message in TrackingAndMethods.QueuedCassieMessages)
				{
					Cassie.Message(message, true, false);
					Log.Debug($"Sending message via cassie: {message}", Subclass.Instance.Config.Debug);
				}
				TrackingAndMethods.QueuedCassieMessages.Clear();
			});
		}

		public void OnRoundEnded(RoundEndedEventArgs ev)
		{
			// I may just consider using reflection and just loop over all members and clear them if I can.
			TrackingAndMethods.KillAllCoroutines();
			TrackingAndMethods.Coroutines.Clear();
			TrackingAndMethods.PlayersWithSubclasses.Clear();
			TrackingAndMethods.Cooldowns.Clear();
			TrackingAndMethods.FriendlyFired.Clear();
			TrackingAndMethods.PlayersThatBypassedTeslaGates.Clear();
			TrackingAndMethods.PreviousRoles.Clear();
			TrackingAndMethods.PlayersWithZombies.Clear();
			TrackingAndMethods.PlayersThatHadZombies.Clear();
			TrackingAndMethods.QueuedCassieMessages.Clear();
			TrackingAndMethods.NextSpawnWave.Clear();
			TrackingAndMethods.NextSpawnWaveGetsRole.Clear();
			TrackingAndMethods.PlayersThatJustGotAClass.Clear();
			TrackingAndMethods.SubClassesSpawned.Clear();
			TrackingAndMethods.PreviousSubclasses.Clear();
			TrackingAndMethods.PreviousBadges.Clear();
			TrackingAndMethods.RagdollRoles.Clear();
			TrackingAndMethods.AbilityUses.Clear();
			TrackingAndMethods.PlayersInvisibleByCommand.Clear();
			TrackingAndMethods.PlayersVenting.Clear();
			TrackingAndMethods.NumSpawnWaves.Clear();
			TrackingAndMethods.SpawnWaveSpawns.Clear();
			TrackingAndMethods.ClassesGiven.Clear();
			TrackingAndMethods.DontGiveClasses.Clear();
			TrackingAndMethods.PlayersBloodLusting.Clear();
			TrackingAndMethods.Zombie106Kills.Clear();
			API.EnableAllClasses();
		}

		public void OnRespawningTeam(RespawningTeamEventArgs ev)
		{
			if (ev.Players.Count == 0 || !ev.IsAllowed) return;
			Team spawnedTeam = ev.NextKnownTeam == SpawnableTeamType.NineTailedFox ? Team.MTF : Team.CHI;
			if (!TrackingAndMethods.NumSpawnWaves.ContainsKey(spawnedTeam)) TrackingAndMethods.NumSpawnWaves.Add(spawnedTeam, 0);
			TrackingAndMethods.NumSpawnWaves[spawnedTeam]++;
			Timing.CallDelayed(5f, () => // Clear them after the wave spawns instead.
			{
				TrackingAndMethods.NextSpawnWave.Clear();
				TrackingAndMethods.NextSpawnWaveGetsRole.Clear();
				TrackingAndMethods.SpawnWaveSpawns.Clear();
			});
			bool ntfSpawning = ev.NextKnownTeam == Respawning.SpawnableTeamType.NineTailedFox;
			if (!Subclass.Instance.Config.AdditiveChance)
			{
				List<RoleType> hasRole = new List<RoleType>();
				foreach (SubClass subClass in Subclass.Instance.Classes.Values.Where(e => e.BoolOptions["Enabled"] &&
				(!e.IntOptions.ContainsKey("MaxSpawnPerRound") || TrackingAndMethods.ClassesSpawned(e) < e.IntOptions["MaxSpawnPerRound"]) &&
				(ntfSpawning ? (e.AffectsRoles.Contains(RoleType.NtfCadet) || e.AffectsRoles.Contains(RoleType.NtfCommander) ||
				e.AffectsRoles.Contains(RoleType.NtfLieutenant)) : e.AffectsRoles.Contains(RoleType.ChaosInsurgency)) &&
				((e.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.BoolOptions["OnlyAffectsSpawnWave"]) ||
				(e.BoolOptions.ContainsKey("AffectsSpawnWave") && e.BoolOptions["AffectsSpawnWave"])) &&
				(!e.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.BoolOptions["WaitForSpawnWaves"] &&
				TrackingAndMethods.GetNumWavesSpawned(e.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
				(Team)Enum.Parse(typeof(Team), e.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.IntOptions["NumSpawnWavesToWait"])) &&
				TrackingAndMethods.EvaluateSpawnParameters(e)))
				{
					if ((ntfSpawning ? (subClass.AffectsRoles.Contains(RoleType.NtfCadet) ||
					subClass.AffectsRoles.Contains(RoleType.NtfCommander) || subClass.AffectsRoles.Contains(RoleType.NtfLieutenant))
					: subClass.AffectsRoles.Contains(RoleType.ChaosInsurgency)) && (rnd.NextDouble() * 100) < subClass.FloatOptions["ChanceToGet"])
					{
						if (ntfSpawning)
						{
							if (!hasRole.Contains(RoleType.NtfCadet) && subClass.AffectsRoles.Contains(RoleType.NtfCadet))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.NtfCadet, subClass);
								hasRole.Add(RoleType.NtfCadet);
							}

							if (!hasRole.Contains(RoleType.NtfLieutenant) && subClass.AffectsRoles.Contains(RoleType.NtfLieutenant))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.NtfLieutenant, subClass);
								hasRole.Add(RoleType.NtfLieutenant);
							}

							if (!hasRole.Contains(RoleType.NtfCommander) && subClass.AffectsRoles.Contains(RoleType.NtfCommander))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.NtfCommander, subClass);
								hasRole.Add(RoleType.NtfCommander);
							}

							if (hasRole.Count == 3) break;
						}
						else
						{
							if (subClass.AffectsRoles.Contains(RoleType.ChaosInsurgency))
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.ChaosInsurgency, subClass);
								break;
							}
						}
					}
				}
			}
			else
			{
				double num = (rnd.NextDouble() * 100);
				if (!ntfSpawning && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.ChaosInsurgency)) return;
				else if (ntfSpawning && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfCadet) &&
					!Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfCommander) && !Subclass.Instance.ClassesAdditive.ContainsKey(RoleType.NtfLieutenant))
					return;

				if (!ntfSpawning)
				{
					foreach (var possibity in Subclass.Instance.ClassesAdditive[RoleType.ChaosInsurgency].Where(e => e.Key.BoolOptions["Enabled"] &&
					(!e.Key.IntOptions.ContainsKey("MaxSpawnPerRound") || TrackingAndMethods.ClassesSpawned(e.Key) < e.Key.IntOptions["MaxSpawnPerRound"]) &&
					((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) ||
					(e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"])) &&
					(!e.Key.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.Key.BoolOptions["WaitForSpawnWaves"] &&
					TrackingAndMethods.GetNumWavesSpawned(e.Key.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
					(Team)Enum.Parse(typeof(Team), e.Key.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.Key.IntOptions["NumSpawnWavesToWait"])) &&
					TrackingAndMethods.EvaluateSpawnParameters(e.Key)))
					{
						Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for next spawn wave", Subclass.Instance.Config.Debug);
						if (num < possibity.Value)
						{
							TrackingAndMethods.NextSpawnWaveGetsRole.Add(RoleType.ChaosInsurgency, possibity.Key);
							break;
						}
						else
						{
							Log.Debug($"Next spawn wave did not get subclass {possibity.Key.Name}", Subclass.Instance.Config.Debug);
						}
					}
				}
				else
				{
					RoleType[] roles = { RoleType.NtfCommander, RoleType.NtfLieutenant, RoleType.NtfCadet };
					foreach (RoleType role in roles)
					{
						foreach (var possibity in Subclass.Instance.ClassesAdditive[role].Where(e => e.Key.BoolOptions["Enabled"] &&
						(!e.Key.IntOptions.ContainsKey("MaxSpawnPerRound") || TrackingAndMethods.ClassesSpawned(e.Key) < e.Key.IntOptions["MaxSpawnPerRound"]) &&
						((e.Key.BoolOptions.ContainsKey("OnlyAffectsSpawnWave") && e.Key.BoolOptions["OnlyAffectsSpawnWave"]) ||
						(e.Key.BoolOptions.ContainsKey("AffectsSpawnWave") && e.Key.BoolOptions["AffectsSpawnWave"])) &&
						(!e.Key.BoolOptions.ContainsKey("WaitForSpawnWaves") || (e.Key.BoolOptions["WaitForSpawnWaves"] &&
						TrackingAndMethods.GetNumWavesSpawned(e.Key.StringOptions.ContainsKey("WaitSpawnWaveTeam") ?
						(Team)Enum.Parse(typeof(Team), e.Key.StringOptions["WaitSpawnWaveTeam"]) : Team.RIP) < e.Key.IntOptions["NumSpawnWavesToWait"]))
						&& TrackingAndMethods.EvaluateSpawnParameters(e.Key)))
						{
							Log.Debug($"Evaluating possible subclass {possibity.Key.Name} for next spawn wave", Subclass.Instance.Config.Debug);
							if (num < possibity.Value)
							{
								TrackingAndMethods.NextSpawnWaveGetsRole.Add(role, possibity.Key);
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
			TrackingAndMethods.NextSpawnWave = ev.Players;
		}

		public void AttemptRevive(SendingConsoleCommandEventArgs ev, SubClass subClass, bool necro = false)
		{
			Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} attempt", Subclass.Instance.Config.Debug);
			AbilityType ability = necro ? AbilityType.Necromancy : AbilityType.Revive;
			if (TrackingAndMethods.OnCooldown(ev.Player, ability, subClass))
			{
				Log.Debug($"Player {ev.Player.Nickname} {(necro ? "necromancy" : "revive")} on cooldown", Subclass.Instance.Config.Debug);
				TrackingAndMethods.DisplayCooldown(ev.Player, necro ? AbilityType.Necromancy : AbilityType.Revive, subClass, necro ? "necromancy" : "revive", Time.time);
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
				if (!necro && TrackingAndMethods.GetPreviousTeam(owner) != null &&
				TrackingAndMethods.GetPreviousTeam(owner) == ev.Player.Team)
				{
					if (TrackingAndMethods.PlayersThatJustGotAClass.ContainsKey(owner)) TrackingAndMethods.PlayersThatJustGotAClass[owner] = Time.time + 3f;
					else TrackingAndMethods.PlayersThatJustGotAClass.Add(owner, Time.time + 3f);

					owner.SetRole((RoleType)TrackingAndMethods.GetPreviousRole(owner), true);

					if (TrackingAndMethods.PreviousSubclasses.ContainsKey(owner) && TrackingAndMethods.PreviousSubclasses[owner].AffectsRoles.Contains((RoleType)TrackingAndMethods.GetPreviousRole(owner)))
						TrackingAndMethods.AddClass(owner, TrackingAndMethods.PreviousSubclasses[owner], false, true);

					owner.Inventory.Clear();
					revived = true;
				}
				else if (necro)
				{
					owner.Role = RoleType.Scp0492;
					TrackingAndMethods.AddZombie(ev.Player, owner);
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
					TrackingAndMethods.AddCooldown(ev.Player, ability);
					TrackingAndMethods.UseAbility(ev.Player, ability, subClass);
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
				grenade.throwLinearVelocityOffset, grenade.throwAngularVelocity, player.Team);
			NetworkServer.Spawn(grenade.gameObject);
		}
	}
}
