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
    class Summon : ICommand
    {
        private System.Random rnd = new System.Random();

        public string Command { get; } = "summon";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Summon zombies from the spectators, if you have the summon ability.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) || !TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Summon))
            {
                Log.Debug($"Player {player.Nickname} could not use summon", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
            if (TrackingAndMethods.OnCooldown(player, AbilityType.Summon, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to summon", Subclass.Instance.Config.Debug);
                TrackingAndMethods.DisplayCooldown(player, AbilityType.Summon, subClass, "summon", Time.time);
                response = "";
                return true;
            }

            if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Summon, subClass))
            {
                TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Summon, subClass, "summon");
                response = "";
                return true;
            }

            int min = subClass.IntOptions.ContainsKey("SummonMinSpawn") ? subClass.IntOptions["SummonMinSpawn"] : 1;
            int max = subClass.IntOptions.ContainsKey("SummonMaxSpawn") ? subClass.IntOptions["SummonMaxSpawn"] : 5;

            List<Player> spectators = Player.List.Where(p => p.Role == RoleType.Spectator).ToList();

            if (spectators.Count == 0)
            {
                player.Broadcast(2, Subclass.Instance.Config.NoAvailableSpectators);
                response = "";
                return true;
            }

            TrackingAndMethods.UseAbility(player, AbilityType.Summon, subClass);
            TrackingAndMethods.AddCooldown(player, AbilityType.Summon);

            int spawns = Mathf.Clamp((int)(rnd.NextDouble() * ((max - min) + 1)) + min, 0, spectators.Count);

            for(int i = 0; i < spawns; i++)
			{
                int index = rnd.Next(spectators.Count);
                Player p = spectators[index];
                spectators.RemoveAt(index);
                p.Role = RoleType.Scp0492;
                p.IsFriendlyFireEnabled = true;
                p.Position = player.Position + new Vector3(rnd.Next(-2, 2), 1, rnd.Next(-2, 2));
                TrackingAndMethods.AddZombie(player, p);
            }

            response = "";
            return true;
        }
    }
}
