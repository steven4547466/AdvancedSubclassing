using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subclass.AbilityCommands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	class Revive : ICommand
	{
		public string Command { get; } = "revive";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Revive a player, if you have the revive ability.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			Log.Debug($"Player {player.Nickname} is attempting to revive", Subclass.Instance.Config.Debug);
			if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) && TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Revive))
			{
				SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
				if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Revive, subClass))
				{
					TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Revive, subClass, "revive");
					response = "";
					return false;
				}
				Utils.AttemptRevive(player, subClass, false);
			}
			response = "";
			return true;
		}
	}
}
