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
    class Grenade : ICommand
    {
        public string Command { get; } = "grenade";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Spawn a frag grenade, if you have the grenade ability.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) && TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.GrenadeOnCommand))
            {
                SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
                if (!TrackingAndMethods.CanUseAbility(player, AbilityType.GrenadeOnCommand, subClass))
                {
                    TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.GrenadeOnCommand, subClass, "grenade on command");
                    response = "";
                    return true;
                }

                if (TrackingAndMethods.OnCooldown(player, AbilityType.GrenadeOnCommand, subClass))
                {
                    Log.Debug($"Player {player.Nickname} failed to grenade on command", Subclass.Instance.Config.Debug);
                    TrackingAndMethods.DisplayCooldown(player, AbilityType.GrenadeOnCommand, subClass, "grenade", Time.time);
                    response = "";
                    return true;
                }

                Utils.SpawnGrenade(ItemType.GrenadeFrag, player, subClass);
                TrackingAndMethods.AddCooldown(player, AbilityType.GrenadeOnCommand);
                TrackingAndMethods.UseAbility(player, AbilityType.GrenadeOnCommand, subClass);
                Log.Debug($"Player {player.Nickname} successfully used grenade on commad", Subclass.Instance.Config.Debug);
            }
            else
            {
                Log.Debug($"Player {player.Nickname} could not grenade on command", Subclass.Instance.Config.Debug);
            }
            response = "";
            return true;
        }
    }
}
