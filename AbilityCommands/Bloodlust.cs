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
using MEC;

namespace Subclass.AbilityCommands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    class Bloodlust : ICommand
    {
        public string Command { get; } = "lust";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Enter a bloodlust and gain speed.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "";
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) ||
                !TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Bloodlust))
            {
                Log.Debug($"Player {player.Nickname} could not use the bloodlust command", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
            if (TrackingAndMethods.OnCooldown(player, AbilityType.Bloodlust, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to use the bloodlust command", Subclass.Instance.Config.Debug);
                TrackingAndMethods.DisplayCooldown(player, AbilityType.Bloodlust, subClass, "bloodlust", Time.time);
                response = "";
                return true;
            }

            if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Bloodlust, subClass))
            {
                TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Bloodlust, subClass, "bloodlust");
                response = "";
                return true;
            }

            Scp207 scp207 = player.ReferenceHub.playerEffectsController.GetEffect<Scp207>();

            byte prevIntensity = scp207.Intensity;
            scp207.ServerChangeIntensity((byte)subClass.IntOptions["BloodlustIntensity"]);
            scp207.ServerChangeDuration(subClass.FloatOptions["BloodlustDuration"], false);

            TrackingAndMethods.PlayersBloodLusting.Add(player);
            TrackingAndMethods.UseAbility(player, AbilityType.Bloodlust, subClass);
            TrackingAndMethods.AddCooldown(player, AbilityType.Bloodlust);

            Timing.CallDelayed(subClass.FloatOptions["BloodlustDuration"], () => 
            {
                if (TrackingAndMethods.PlayersBloodLusting.Contains(player)) TrackingAndMethods.PlayersBloodLusting.Remove(player);
                scp207.ServerChangeIntensity(prevIntensity);
                scp207.ServerChangeDuration(float.MaxValue, false);
            });

            return true;
        }
    }
}
