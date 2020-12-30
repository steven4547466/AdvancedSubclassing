using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using RemoteAdmin;

namespace Subclass.Commands
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	class Enable : ICommand
	{
		public string Command { get; } = "enablesubclass";

		public string[] Aliases { get; } = { "esc" };

		public string Description { get; } = "Enables subclasses.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (sender is PlayerCommandSender player)
			{
				if (!player.CheckPermission("sc.enable"))
				{
					response = "You do not have the necessary permissions to run this command. Requires: sc.enable";
					return false;
				}

				if (arguments.Count == 0)
				{
					API.EnableAllClasses();
				}
				else
				{
					if (!API.EnableClass(string.Join(" ", arguments)))
					{
						response = "Subclass not found";
						return false;
					}
				}

				response = "Enabled";

				return true;

			}
			else
			{
				if (arguments.Count == 0)
				{
					API.EnableAllClasses();
				}
				else
				{
					if (!API.EnableClass(string.Join(" ", arguments)))
					{
						response = "Subclass not found";
						return false;
					}
				}
				response = "Enabled";

				return true;
			}
		}
	}
}
