using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;

namespace Subclass.Commands
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	class GiveSubclass : ICommand
	{
		public string Command { get; } = "subclass";

		public string[] Aliases { get; } = { "gsc" };

		public string Description { get; } = "Gives a player a subclass";

		Random rnd = new Random();

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (sender is PlayerCommandSender player)
			{
				Player p = Player.Get(player.SenderId);
				if (!p.CheckPermission("sc.giveclass"))
				{
					response = "You do not have the necessary permissions to run this command. Requires: sc.giveclass";
					return false;
				}

				if (arguments.Count == 0)
				{
					response = "Command syntax should be subclass (player id/all) [class].";
					return false;
				}

				try
				{
					if (Player.Get(int.Parse(arguments.Array[arguments.Offset])) != null)
					{
						Player player1 = Player.Get(int.Parse(arguments.Array[arguments.Offset]));
						if (!Subclass.Instance.Classes.ContainsKey(string.Join(" ", arguments.Array.Segment(arguments.Offset + 1))))
						{
							response = "Class not found.";
							return false;
						}
						else
						{
							SubClass sc = Subclass.Instance.Classes[string.Join(" ", arguments.Array.Segment(arguments.Offset + 1))];
							if (!sc.AffectsRoles.Contains(player1.Role)) player1.SetRole(sc.AffectsRoles[rnd.Next(sc.AffectsRoles.Count)], true);
							TrackingAndMethods.RemoveAndAddRoles(player1, true);
							TrackingAndMethods.AddClass(player1, sc);
							response = "Success.";
							return true;
						}
					}
					else
					{
						response = "Player not found.";
						return false;
					}
				}
				catch
				{
					if (arguments.Array[arguments.Offset].ToLower() != "all")
					{
						if (!Subclass.Instance.Classes.ContainsKey(string.Join(" ", arguments.Array.Segment(arguments.Offset))))
						{
							response = "Class not found.";
							return false;
						}
						else
						{
							SubClass sc = Subclass.Instance.Classes[string.Join(" ", arguments.Array.Segment(arguments.Offset))];
							if (!sc.AffectsRoles.Contains(p.Role)) p.SetRole(sc.AffectsRoles[rnd.Next(sc.AffectsRoles.Count)], true);
							TrackingAndMethods.RemoveAndAddRoles(p, true);
							TrackingAndMethods.AddClass(p, sc);
							response = "Success.";
							return true;
						}
					}
					else
					{
						if (!Subclass.Instance.Classes.ContainsKey(string.Join(" ", arguments.Array.Segment(arguments.Offset + 1))))
						{
							response = "Class not found.";
							return false;
						}
						else
						{
							SubClass sc = Subclass.Instance.Classes[string.Join(" ", arguments.Array.Segment(arguments.Offset + 1))];
							foreach (Player p1 in Player.List)
							{
								if (p1.Role == RoleType.Spectator) continue;
								if (!sc.AffectsRoles.Contains(p1.Role)) p1.SetRole(sc.AffectsRoles[rnd.Next(sc.AffectsRoles.Count)], true);
								TrackingAndMethods.RemoveAndAddRoles(p1, true);
								TrackingAndMethods.AddClass(p1, sc);
							}
							response = "Success.";
							return true;
						}
					}
				}
			}
			response = "";
			return false;
		}
	}
}
