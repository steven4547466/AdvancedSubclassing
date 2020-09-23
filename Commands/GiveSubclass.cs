using System;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using Microsoft.SqlServer.Server;
using RemoteAdmin;

namespace Subclass.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    class GiveSubclass : ICommand
    {
        public string Command { get; } = "subclass";

        public string[] Aliases { get; } = { "gsc" };

        public string Description { get; } = "Gives a player a subclass";

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
                    response = "Command syntax should be subclass (player id) [class].";
                    return false;
                }

                try
                {
                    if(Player.Get(Int32.Parse(arguments.Array[arguments.Offset])) != null)
                    {
                        Player player1 = Player.Get(Int32.Parse(arguments.Array[arguments.Offset]));
                        if (!Subclass.Instance.Classes.ContainsKey(String.Join(" ", arguments.Array.Segment(arguments.Offset + 1))))
                        {
                            response = "Class not found.";
                            return false;
                        }
                        else
                        {
                            if (!Subclass.Instance.Classes[String.Join(" ", arguments.Array.Segment(arguments.Offset + 1))].AffectsRoles.Contains(player1.Role))
                            {
                                response = "They are not the proper role for this class.";
                                return false;
                            }
                            else
                            {
                                Tracking.RemoveAndAddRoles(player1, true);
                                Tracking.AddClass(player1, Subclass.Instance.Classes[String.Join(" ", arguments.Array.Segment(arguments.Offset + 1))]);
                                response = "Success.";
                                return true;
                            }
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
                    if (!Subclass.Instance.Classes.ContainsKey(String.Join(" ", arguments.Array.Segment(arguments.Offset))))
                    {
                        response = "Class not found.";
                        return false;
                    }
                    else
                    {
                        if (!Subclass.Instance.Classes[String.Join(" ", arguments.Array.Segment(arguments.Offset))].AffectsRoles.Contains(p.Role))
                        {
                            response = "You are not the proper role for this class.";
                            return false;
                        }
                        else
                        {
                            Tracking.RemoveAndAddRoles(p, true);
                            Tracking.AddClass(p, Subclass.Instance.Classes[String.Join(" ", arguments.Array.Segment(arguments.Offset))]);
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
