using CommandSystem;
using Exiled.API.Features;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CustomPlayerEffects;

namespace Subclass.AbilityCommands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    class Corrupt : ICommand
    {
        public string Command { get; } = "corrupt";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Slows players around you.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "";
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) ||
                !TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Corrupt))
            {
                Log.Debug($"Player {player.Nickname} could not use the corrupt command", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
            if (TrackingAndMethods.OnCooldown(player, AbilityType.Corrupt, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to use the corrupt command", Subclass.Instance.Config.Debug);
                TrackingAndMethods.DisplayCooldown(player, AbilityType.Corrupt, subClass, "corrupt", Time.time);
                response = "";
                return true;
            }

            if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Corrupt, subClass))
            {
                TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Corrupt, subClass, "corrupt");
                response = "";
                return true;
            }

            
            Collider[] colliders = Physics.OverlapSphere(player.Position, (subClass.FloatOptions.ContainsKey("CorruptRange") ? subClass.FloatOptions["CorruptRange"] : 10f));
            IEnumerable<Player> players = colliders.Select(c => Player.Get(c.gameObject)).Distinct();
            if (players.Count() > 0)
            {
                TrackingAndMethods.UseAbility(player, AbilityType.Corrupt, subClass);
                TrackingAndMethods.AddCooldown(player, AbilityType.Corrupt);
                foreach (Player target in players)
                {
                    if (target == null || target.Id == player.Id || player.Side == target.Side || (player.Team == Team.SCP && target.Team == Team.TUT)) continue;
                    target.EnableEffect<SinkHole>((subClass.FloatOptions.ContainsKey("CorruptDuration") ? subClass.FloatOptions["CorruptDuration"] : 3));
                }
            }

            return true;
        }
    }
}
