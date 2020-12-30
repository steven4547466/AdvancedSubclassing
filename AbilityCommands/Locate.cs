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
	class Locate : ICommand
	{
		public string Command { get; } = "locate";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Use echolocation, if you have the echolocation ability.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			if (player.Role != RoleType.Scp93953 && player.Role != RoleType.Scp93989 &&
				(!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) ||
				!TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Scp939Vision)))
			{
				Log.Debug($"Player {player.Nickname} failed to echolocate", Subclass.Instance.Config.Debug);
				response = "";
				return true;
			}

			if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) && TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Echolocate))
			{
				SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
				if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Echolocate, subClass))
				{
					TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Echolocate, subClass, "echolocate");
					response = "";
					return true;
				}
				if (TrackingAndMethods.OnCooldown(player, AbilityType.Echolocate, subClass))
				{
					Log.Debug($"Player {player.Nickname} failed to echolocate", Subclass.Instance.Config.Debug);
					TrackingAndMethods.DisplayCooldown(player, AbilityType.Echolocate, subClass, "echolocation", Time.time);
					response = "";
					return true;
				}

				Collider[] lcolliders = Physics.OverlapSphere(player.Position, subClass.FloatOptions.ContainsKey("EcholocateRadius") ? subClass.FloatOptions["EcholocateRadius"] : 10f);

				foreach (Collider PlayerCollider in lcolliders.Where(c => Player.Get(c.gameObject) != null))
				{
					Player.Get(PlayerCollider.gameObject).ReferenceHub.footstepSync?.CmdScp939Noise(100f);
				}

				TrackingAndMethods.AddCooldown(player, AbilityType.Echolocate);
				TrackingAndMethods.UseAbility(player, AbilityType.Echolocate, subClass);
				Log.Debug($"Player {player.Nickname} successfully used echolocate", Subclass.Instance.Config.Debug);
			}
			response = "";
			return true;
		}
	}
}
