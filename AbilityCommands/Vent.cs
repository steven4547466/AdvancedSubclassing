using CommandSystem;
using CustomPlayerEffects;
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
    class Vent : ICommand
    {
        public string Command { get; } = "vent";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Begin venting, if you have the vent ability.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            Scp268 scp268 = player.ReferenceHub.playerEffectsController.GetEffect<Scp268>();
            if (Tracking.PlayersVenting.Contains(player) && scp268 != null && scp268.Enabled)
            {
                Tracking.PlayersVenting.Remove(player);
                scp268.ServerDisable();
                response = "";
                return true;
            }

            if (!Tracking.PlayersWithSubclasses.ContainsKey(player) || !Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Vent))
            {
                Log.Debug($"Player {player.Nickname} could not vent", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = Tracking.PlayersWithSubclasses[player];
            if (Tracking.OnCooldown(player, AbilityType.Vent, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to vent", Subclass.Instance.Config.Debug);
                Tracking.DisplayCooldown(player, AbilityType.Vent, subClass, "vent", Time.time);
                response = "";
                return true;
            }

            if (!Tracking.CanUseAbility(player, AbilityType.Vent, subClass))
            {
                Tracking.DisplayCantUseAbility(player, AbilityType.Vent, subClass, "vent");
                response = "";
                return true;
            }

            if (scp268 != null)
            {
                if (scp268.Enabled)
                {
                    Log.Debug($"Player {player.Nickname} failed to vent", Subclass.Instance.Config.Debug);
                    player.Broadcast(3, Subclass.Instance.Config.AlreadyInvisibleMessage);
                    response = "";
                    return true;
                }

                //scp268.Duration = subClass.FloatOptions.ContainsKey("InvisibleOnCommandDuration") ?
                //    subClass.FloatOptions["InvisibleOnCommandDuration"]*2f : 30f*2f;

                //player.ReferenceHub.playerEffectsController.EnableEffect(scp268);

                player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
                Tracking.PlayersInvisibleByCommand.Add(player);
                Tracking.PlayersVenting.Add(player);
                Timing.CallDelayed(subClass.FloatOptions.ContainsKey("VentDuration") ?
                    subClass.FloatOptions["VentDuration"] : 15f, () =>
                    {
                        if (Tracking.PlayersVenting.Contains(player)) Tracking.PlayersVenting.Remove(player);
                        if (Tracking.PlayersInvisibleByCommand.Contains(player)) Tracking.PlayersInvisibleByCommand.Remove(player);
                        if (scp268.Enabled) player.ReferenceHub.playerEffectsController.DisableEffect<Scp268>();
                    });

                Tracking.AddCooldown(player, AbilityType.Vent);
                Tracking.UseAbility(player, AbilityType.Vent, subClass);

            }
            response = "";
            return true;
        }
    }
}
