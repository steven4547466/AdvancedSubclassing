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
	class Necro : ICommand
	{
		public string Command { get; } = "necro";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Revive a player as a zombie, if you have the necromancy ability.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			Log.Debug($"Player {player.Nickname} is attempting to necro", Subclass.Instance.Config.Debug);
			if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) && TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Necromancy))
			{
				SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
				if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Necromancy, subClass))
				{
					TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Necromancy, subClass, "necro");
					response = "";
					return false;
				}
				Utils.AttemptRevive(player, subClass, true);
			}
			response = "";
			return true;
		}
	}
}
