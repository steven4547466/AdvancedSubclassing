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
    class Invis : ICommand
    {
        public string Command { get; } = "invis";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Go invisible, if you have the invisibility ability.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (!Tracking.PlayersWithSubclasses.ContainsKey(player) || !Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.InvisibleOnCommand))
            {
                Log.Debug($"Player {player.Nickname} could not go invisible on command", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            Scp268 scp268 = player.ReferenceHub.playerEffectsController.GetEffect<Scp268>();
            if (scp268 != null)
            {
                SubClass subClass = Tracking.PlayersWithSubclasses[player];
                if (!Tracking.CanUseAbility(player, AbilityType.InvisibleOnCommand, subClass))
                {
                    Tracking.DisplayCantUseAbility(player, AbilityType.InvisibleOnCommand, subClass, "invisible on command");
                    response = "";
                    return true;
                }

                if (scp268.Enabled)
                {
                    Log.Debug($"Player {player.Nickname} failed to go invisible on command", Subclass.Instance.Config.Debug);
                    player.Broadcast(3, Subclass.Instance.Config.AlreadyInvisibleMessage);
                    response = "";
                    return true;
                }

                if (Tracking.OnCooldown(player, AbilityType.InvisibleOnCommand, subClass))
                {
                    Log.Debug($"Player {player.Nickname} failed to go invisible on command", Subclass.Instance.Config.Debug);
                    Tracking.DisplayCooldown(player, AbilityType.InvisibleOnCommand, subClass, "invisible", Time.time);
                    response = "";
                    return true;
                }

                //scp268.Duration = subClass.FloatOptions.ContainsKey("InvisibleOnCommandDuration") ?
                //    subClass.FloatOptions["InvisibleOnCommandDuration"]*2f : 30f*2f;

                //player.ReferenceHub.playerEffectsController.EnableEffect(scp268);

                player.ReferenceHub.playerEffectsController.EnableEffect<Scp268>();
                Tracking.PlayersInvisibleByCommand.Add(player);
                Timing.CallDelayed(subClass.FloatOptions.ContainsKey("InvisibleOnCommandDuration") ?
                    subClass.FloatOptions["InvisibleOnCommandDuration"] : 30f, () =>
                    {
                        if (Tracking.PlayersInvisibleByCommand.Contains(player)) Tracking.PlayersInvisibleByCommand.Remove(player);
                        if (scp268.Enabled) player.ReferenceHub.playerEffectsController.DisableEffect<Scp268>();
                    });

                Tracking.AddCooldown(player, AbilityType.InvisibleOnCommand);
                Tracking.UseAbility(player, AbilityType.InvisibleOnCommand, subClass);

            }
            response = "";
            return true;
        }
    }
}
