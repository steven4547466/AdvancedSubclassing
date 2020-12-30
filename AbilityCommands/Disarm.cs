using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.AbilityCommands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	class Disarm : ICommand
	{
		public string Command { get; } = "disarm";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Disarms the player you're looking at or players around you.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			response = "";
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) ||
				!TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disarm) ||
				player.IsCuffed)
			{
				Log.Debug($"Player {player.Nickname} could not use the disarm command", Subclass.Instance.Config.Debug);
				response = "";
				return true;
			}
			SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
			if (TrackingAndMethods.OnCooldown(player, AbilityType.Disarm, subClass))
			{
				Log.Debug($"Player {player.Nickname} failed to use the disarm command", Subclass.Instance.Config.Debug);
				TrackingAndMethods.DisplayCooldown(player, AbilityType.Disarm, subClass, "disarm", Time.time);
				response = "";
				return true;
			}

			if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Disarm, subClass))
			{
				TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Disarm, subClass, "disarm");
				response = "";
				return true;
			}

			if (!subClass.BoolOptions.ContainsKey("DisarmSphere") || !subClass.BoolOptions["DisarmSphere"])
			{
				if (Physics.Raycast(player.CameraTransform.position, player.CameraTransform.forward, out RaycastHit hit,
					(subClass.FloatOptions.ContainsKey("DisarmRange") ? subClass.FloatOptions["DisarmRange"] : 1.3f)))
				{
					Player target = Player.Get(hit.collider.gameObject) ?? Player.Get(hit.collider.GetComponentInParent<ReferenceHub>());
					if (target == null || target.Id == player.Id || player.Side == target.Side || (player.Team == Team.SCP && target.Team == Team.TUT)) return true;
					TrackingAndMethods.UseAbility(player, AbilityType.Disarm, subClass);
					TrackingAndMethods.AddCooldown(player, AbilityType.Disarm);
					if (target.CurrentItemIndex != -1) target.DropItem(target.CurrentItem);
				}
			}
			else
			{
				Collider[] colliders = Physics.OverlapSphere(player.Position, (subClass.FloatOptions.ContainsKey("DisarmRange") ? subClass.FloatOptions["DisarmRange"] : 3f));
				IEnumerable<Player> players = colliders.Select(c => Player.Get(c.gameObject)).Distinct();
				if (players.Count() > 0)
				{
					TrackingAndMethods.UseAbility(player, AbilityType.Disarm, subClass);
					TrackingAndMethods.AddCooldown(player, AbilityType.Disarm);
					foreach (Player target in players)
					{
						if (target == null || target.Id == player.Id || player.Side == target.Side || (player.Team == Team.SCP && target.Team == Team.TUT)) continue;
						if (target.CurrentItemIndex != -1) target.DropItem(target.CurrentItem);
					}
				}
			}

			return true;
		}
	}
}
