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
	class Flash : ICommand
	{
		public string Command { get; } = "flash";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Spawn a flash grenade, if you have the flash ability.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) && TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.FlashOnCommand))
			{
				SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
				if (!TrackingAndMethods.CanUseAbility(player, AbilityType.FlashOnCommand, subClass))
				{
					TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.FlashOnCommand, subClass, "flash on command");
					response = "";
					return true;
				}

				if (TrackingAndMethods.OnCooldown(player, AbilityType.FlashOnCommand, subClass))
				{
					Log.Debug($"Player {player.Nickname} failed to flash on command", Subclass.Instance.Config.Debug);
					TrackingAndMethods.DisplayCooldown(player, AbilityType.FlashOnCommand, subClass, "flash", Time.time);
					response = "";
					return true;
				}

				Utils.SpawnGrenade(ItemType.GrenadeFlash, player, subClass);
				TrackingAndMethods.AddCooldown(player, AbilityType.FlashOnCommand);
				TrackingAndMethods.UseAbility(player, AbilityType.FlashOnCommand, subClass);
				Log.Debug($"Player {player.Nickname} successfully used flash on commad", Subclass.Instance.Config.Debug);
			}
			else
			{
				Log.Debug($"Player {player.Nickname} could not flash on command", Subclass.Instance.Config.Debug);
			}
			response = "";
			return true;
		}
	}
}
