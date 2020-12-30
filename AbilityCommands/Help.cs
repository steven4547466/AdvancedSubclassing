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
	class Help : ICommand
	{
		public string Command { get; } = "schelp";

		public string[] Aliases { get; } = { };

		public string Description { get; } = "Get help on your current subclass, or any subclass";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
			if (arguments.Count == 0)
			{
				if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player))
				{
					response = Subclass.Instance.Config.HelpNoArgumentsProvided;
					return true;
				}

				if (!TrackingAndMethods.PlayersWithSubclasses[player].StringOptions.ContainsKey("HelpMessage"))
				{
					response = Subclass.Instance.Config.HelpNoHelpFound;
					return true;
				}

				response = TrackingAndMethods.PlayersWithSubclasses[player].StringOptions["HelpMessage"];
				return true;
			}
			string sc = string.Join(" ", arguments).ToLower();
			SubClass c = Subclass.Instance.Classes.FirstOrDefault(s => s.Key.ToLower() == sc).Value;

			if (c == null)
			{
				response = Subclass.Instance.Config.HelpNoClassFound;
				return true;
			}

			if (!c.StringOptions.ContainsKey("HelpMessage"))
			{
				response = Subclass.Instance.Config.HelpNoHelpFound;
				return true;
			}

			response = c.StringOptions["HelpMessage"];
			return true;
		}
	}
}
