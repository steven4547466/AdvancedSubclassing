using CommandSystem;
using Exiled.API.Features;
using MEC;
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
	class Noclip : ICommand
	{
		public string Command { get; } = "noclip";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Noclip, if you have the noclip ability.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			Log.Debug($"Player {player.Nickname} is attempting to noclip", Subclass.Instance.Config.Debug);
			if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) && TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.NoClip))
			{
				SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
				if (!TrackingAndMethods.CanUseAbility(player, AbilityType.NoClip, subClass))
				{
					TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.NoClip, subClass, "noclip");
					response = "";
					return true;
				}
				if (TrackingAndMethods.OnCooldown(player, AbilityType.NoClip, subClass))
				{
					Log.Debug($"Player {player.Nickname} failed to noclip - cooldown", Subclass.Instance.Config.Debug);
					TrackingAndMethods.DisplayCooldown(player, AbilityType.NoClip, subClass, "noclip", Time.time);
					response = "";
					return true;
				}
				bool previous = player.NoClipEnabled;
				player.NoClipEnabled = !player.NoClipEnabled;
				TrackingAndMethods.AddCooldown(player, AbilityType.NoClip);
				TrackingAndMethods.UseAbility(player, AbilityType.NoClip, subClass);
				if (subClass.FloatOptions.ContainsKey("NoClipTime"))
				{
					Timing.CallDelayed(subClass.FloatOptions["NoClipTime"], () =>
					{
						if (player.NoClipEnabled != previous) player.NoClipEnabled = previous;
					});
				}
				Log.Debug($"Player {player.Nickname} successfully noclipped", Subclass.Instance.Config.Debug);
			}
			else
			{
				Log.Debug($"Player {player.Nickname} could not noclip", Subclass.Instance.Config.Debug);
			}
			response = "";
			return true;
		}
	}
}
