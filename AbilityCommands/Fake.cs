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
using Mirror;
using MEC;

namespace Subclass.AbilityCommands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    class Fake : ICommand
    {
        public string Command { get; } = "fake";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Fakes your death for a small period of time.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "";
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) ||
                !TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Fake))
            {
                Log.Debug($"Player {player.Nickname} could not use the fake command", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
            if (TrackingAndMethods.OnCooldown(player, AbilityType.Fake, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to use the fake command", Subclass.Instance.Config.Debug);
                TrackingAndMethods.DisplayCooldown(player, AbilityType.Fake, subClass, "fake", Time.time);
                response = "";
                return true;
            }

            if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Fake, subClass))
            {
                TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Fake, subClass, "fake");
                response = "";
                return true;
            }

            Role role = player.ReferenceHub.characterClassManager.Classes.SafeGet((int)player.Role);
            if (role.model_ragdoll == null)
                return false;

            player.EnableEffect<Ensnared>(subClass.FloatOptions.ContainsKey("FakeDuration") ? subClass.FloatOptions["FakeDuration"] : 3);
            player.EnableEffect<Scp268>(subClass.FloatOptions.ContainsKey("FakeDuration") ? subClass.FloatOptions["FakeDuration"] : 3);

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(role.model_ragdoll, player.Position + role.ragdoll_offset.position, Quaternion.Euler(player.GameObject.transform.rotation.eulerAngles + role.ragdoll_offset.rotation));
            NetworkServer.Spawn(gameObject);
            Ragdoll component = gameObject.GetComponent<Ragdoll>();
            component.Networkowner = new Ragdoll.Info(player.UserId, player.Nickname, new PlayerStats.HitInfo(0, player.Nickname, DamageTypes.Falldown, player.Id), 
                role, player.Id);
            component.NetworkallowRecall = false;
            component.NetworkPlayerVelo = (player.ReferenceHub.playerMovementSync == null) ? Vector3.zero : player.ReferenceHub.playerMovementSync.PlayerVelocity;

            TrackingAndMethods.UseAbility(player, AbilityType.Fake, subClass);
            TrackingAndMethods.AddCooldown(player, AbilityType.Fake);

            Timing.CallDelayed(subClass.FloatOptions.ContainsKey("FakeDuration") ? subClass.FloatOptions["FakeDuration"] : 3, () =>
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            });

            return true;
        }
    }
}
