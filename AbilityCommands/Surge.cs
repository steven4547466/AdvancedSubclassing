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
    class Surge : ICommand
    {
        public string Command { get; } = "surge";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Start a local power surge, if you have the power surge ability.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (!Tracking.PlayersWithSubclasses.ContainsKey(player) || !Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.PowerSurge))
            {
                Log.Debug($"Player {player.Nickname} could not use power surge", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = Tracking.PlayersWithSubclasses[player];
            if (Tracking.OnCooldown(player, AbilityType.PowerSurge, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to power surge", Subclass.Instance.Config.Debug);
                Tracking.DisplayCooldown(player, AbilityType.PowerSurge, subClass, "power surge", Time.time);
                response = "";
                return true;
            }

            if (!Tracking.CanUseAbility(player, AbilityType.PowerSurge, subClass))
            {
                Tracking.DisplayCantUseAbility(player, AbilityType.PowerSurge, subClass, "power surge");
                response = "";
                return true;
            }

            float radius = subClass.FloatOptions.ContainsKey("PowerSurgeRadius") ? subClass.FloatOptions["PowerSurgeRadius"] : 30f;
            foreach (Room room in Exiled.API.Features.Map.Rooms)
            {
                if (Vector3.Distance(room.Position, player.Position) <= radius)
                {
                    room.TurnOffLights(subClass.FloatOptions.ContainsKey("PowerSurgeDuration") ? subClass.FloatOptions["PowerSurgeDuration"] : 15f);
                }
            }

            Tracking.AddCooldown(player, AbilityType.PowerSurge);
            Tracking.UseAbility(player, AbilityType.PowerSurge, subClass);
            response = "";
            return true;
        }
    }
}
