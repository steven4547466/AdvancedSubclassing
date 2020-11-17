using CommandSystem;
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
    class Disguise : ICommand
    {
        public string Command { get; } = "disguise";

        public string[] Aliases { get; } = { };

        public string Description { get; } = "Enter a disguise, if you have the disguise ability.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(((PlayerCommandSender)sender).SenderId);
            if (!Tracking.PlayersWithSubclasses.ContainsKey(player) || !Tracking.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disguise))
            {
                Log.Debug($"Player {player.Nickname} could not disguise", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = Tracking.PlayersWithSubclasses[player];
            if (Tracking.OnCooldown(player, AbilityType.Disguise, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to disguise", Subclass.Instance.Config.Debug);
                Tracking.DisplayCooldown(player, AbilityType.Disguise, subClass, "disguise", Time.time);
                response = "";
                return true;
            }

            if (!Tracking.CanUseAbility(player, AbilityType.Disguise, subClass))
            {
                Tracking.DisplayCantUseAbility(player, AbilityType.Disguise, subClass, "disguise");
                response = "";
                return true;
            }

            Team mostTeam = Team.RIP;
            Dictionary<Team, int> occurrences = new Dictionary<Team, int>();
            Collider[] dcolliders = Physics.OverlapSphere(player.Position, 50);
            foreach (Collider c in dcolliders.Where(c => c.enabled && Player.Get(c.gameObject) != null))
            {
                Team t = Player.Get(c.gameObject).Team;
                if (t == Team.CDP) t = Team.CHI;
                if (t == Team.RSC) t = Team.MTF;
                if (!occurrences.ContainsKey(t)) occurrences.Add(t, 0);
                occurrences[t]++;
            }
            var copy = occurrences.ToList();
            copy.Sort((x, y) => y.Value.CompareTo(x.Value));
            mostTeam = copy[0].Key;
            if (mostTeam == player.Team || mostTeam == Team.RIP || mostTeam == Team.SCP)
            {
                Log.Debug($"Player {player.Nickname} failed to disguise", Subclass.Instance.Config.Debug);
                player.Broadcast(3, Subclass.Instance.Config.DisguiseFailedMessage);
                response = "";
                return true;
            }
            RoleType role = RoleType.None;
            switch (mostTeam)
            {
                case Team.CDP:
                    role = RoleType.ClassD;
                    break;

                case Team.CHI:
                    role = RoleType.ChaosInsurgency;
                    break;

                case Team.MTF:
                    role = RoleType.NtfCadet;
                    break;

                case Team.RSC:
                    role = RoleType.Scientist;
                    break;

                case Team.TUT:
                    role = RoleType.Tutorial;
                    break;
            }
            Tracking.PlayersThatJustGotAClass[player] = Time.time + 3f;
            RoleType trueRole = player.Role;
            Round.IsLocked = true;
            player.SetRole(role, true);
            Tracking.AddCooldown(player, AbilityType.Disguise);
            Tracking.UseAbility(player, AbilityType.Disguise, subClass);
            Timing.CallDelayed(Tracking.PlayersWithSubclasses[player].FloatOptions["DisguiseDuration"], () =>
            {
                Tracking.PlayersThatJustGotAClass[player] = Time.time + 3f;
                player.SetRole(trueRole, true);
                Round.IsLocked = false;
            });
            response = "";
            return true;
        }
    }
}
