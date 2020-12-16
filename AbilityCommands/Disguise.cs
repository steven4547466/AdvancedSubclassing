using CommandSystem;
using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using RemoteAdmin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            if (!TrackingAndMethods.PlayersWithSubclasses.ContainsKey(player) || !TrackingAndMethods.PlayersWithSubclasses[player].Abilities.Contains(AbilityType.Disguise))
            {
                Log.Debug($"Player {player.Nickname} could not disguise", Subclass.Instance.Config.Debug);
                response = "";
                return true;
            }
            SubClass subClass = TrackingAndMethods.PlayersWithSubclasses[player];
            if (TrackingAndMethods.OnCooldown(player, AbilityType.Disguise, subClass))
            {
                Log.Debug($"Player {player.Nickname} failed to disguise", Subclass.Instance.Config.Debug);
                TrackingAndMethods.DisplayCooldown(player, AbilityType.Disguise, subClass, "disguise", Time.time);
                response = "";
                return true;
            }

            if (!TrackingAndMethods.CanUseAbility(player, AbilityType.Disguise, subClass))
            {
                TrackingAndMethods.DisplayCantUseAbility(player, AbilityType.Disguise, subClass, "disguise");
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

            bool wasLockedBefore = Round.IsLocked;
            Round.IsLocked = true;

            TrackingAndMethods.AddCooldown(player, AbilityType.Disguise);
            TrackingAndMethods.UseAbility(player, AbilityType.Disguise, subClass);

            TrackingAndMethods.PlayersThatJustGotAClass[player] = Time.time + 5f;
            TrackingAndMethods.RemoveAndAddRoles(player, true, false, false, true);

            float health = player.Health;
            float armor = player.AdrenalineHealth;
            int maxHealth = player.MaxHealth;
            int maxArmor = player.MaxAdrenalineHealth;

            RoleType trueRole = player.Role;

            SubClass cloneClass = new SubClass(subClass);
            cloneClass.BoolOptions["TakesFriendlyFire"] = true;

            player.SetRole(role, true);

            Timing.CallDelayed(0.1f, () =>
            {
                player.Health = health;
                player.AdrenalineHealth = armor;
                player.IsFriendlyFireEnabled = true;
                Player scp035 = null;
                if (Subclass.Instance.Scp035Enabled)
                    scp035 = (Player)Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035")?.Assembly?.GetType("scp035.API.Scp035Data")
                    ?.GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                TrackingAndMethods.AddClass(player, cloneClass, scp035?.Id == player.Id, true, false, true);
            });

            if (subClass.StringOptions.ContainsKey("Badge") && player.RankName == subClass.StringOptions["Badge"])
                player.RankName = null;
            if (subClass.StringOptions.ContainsKey("BadgeColor") && player.RankColor == subClass.StringOptions["BadgeColor"])
                player.RankColor = null;
            Timing.CallDelayed(subClass.FloatOptions["DisguiseDuration"], () =>
            {
                TrackingAndMethods.PlayersThatJustGotAClass[player] = Time.time + 5f;
                TrackingAndMethods.RemoveAndAddRoles(player, true, false, false, true);

                float curHealth = player.Health;
                float curArmor = player.AdrenalineHealth;

                player.SetRole(trueRole, true);

                Timing.CallDelayed(0.1f, () =>
                {
                    Player scp035 = null;
                    if (Subclass.Instance.Scp035Enabled)
                        scp035 = (Player)Loader.Plugins.FirstOrDefault(pl => pl.Name == "scp035")?.Assembly?.GetType("scp035.API.Scp035Data")
                        ?.GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);

                    TrackingAndMethods.AddClass(player, subClass, scp035?.Id == player.Id, true, false, true);

                    player.MaxHealth = maxHealth;
                    player.MaxAdrenalineHealth = maxArmor;
                    player.Health = curHealth;
                    player.AdrenalineHealth = curArmor;
                    player.IsFriendlyFireEnabled = !subClass.BoolOptions["DisregardHasFF"] && subClass.BoolOptions["HasFriendlyFire"];
                });

                if (subClass.StringOptions.ContainsKey("Badge") && player.RankName == null)
                    player.RankName = subClass.StringOptions["Badge"];
                if (subClass.StringOptions.ContainsKey("BadgeColor") && player.RankColor == null)
                    player.RankColor = subClass.StringOptions["BadgeColor"];

                Round.IsLocked = wasLockedBefore;
            });
            response = "";
            return true;
        }
    }
}
