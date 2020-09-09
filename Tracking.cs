using Exiled.API.Extensions;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass
{
    public class Tracking
    {
        public static Dictionary<Player, SubClass> PlayersWithSubclasses = new Dictionary<Player, SubClass>();

        public static Dictionary<Player, Dictionary<AbilityType, float>> Cooldowns = new Dictionary<Player, Dictionary<AbilityType, float>>();

        public static Dictionary<Player, float> PlayersThatBypassedTeslaGates = new Dictionary<Player, float>();

        public static Dictionary<Player, RoleType> PreviousRoles = new Dictionary<Player, RoleType>();

        public static List<Player> FriendlyFired = new List<Player>();

        public static List<string> QueuedCassieMessages = new List<string>();

        public static float RoundStartedAt = 0f;


        public static void RemoveAndAddRoles(Player p, bool dontAddRoles = false)
        {
            if (RoundJustStarted()) return;
            if (PlayersWithSubclasses.ContainsKey(p)) PlayersWithSubclasses.Remove(p);
            if (Cooldowns.ContainsKey(p)) Cooldowns.Remove(p);
            if (FriendlyFired.Contains(p)) FriendlyFired.RemoveAll(e => e == p);
            if (PlayersWithSubclasses.ContainsKey(p) && PlayersWithSubclasses[p].Abilities.Contains(AbilityType.Disable096Trigger) 
                && Scp096.TurnedPlayers.Contains(p)) Scp096.TurnedPlayers.Remove(p);
            if (PlayersWithSubclasses.ContainsKey(p) && PlayersWithSubclasses[p].Abilities.Contains(AbilityType.Disable173Stop) 
                && Scp173.TurnedPlayers.Contains(p)) Scp173.TurnedPlayers.Remove(p);
            if (PlayersWithSubclasses.ContainsKey(p) && PlayersWithSubclasses[p].Abilities.Contains(AbilityType.NoArmorDecay))
                p.ReferenceHub.playerStats.artificialHpDecay = 0.75f;


            if (p.GameObject.GetComponent<MonoBehaviours.InfiniteSprint>() != null)
            {
                Log.Debug($"Player {p.Nickname} has infinite stamina, destroying", Subclass.Instance.Config.Debug);
                p.GameObject.GetComponent<MonoBehaviours.InfiniteSprint>().Destroy();
                p.IsUsingStamina = true; // Have to set it to true for it to remove fully... for some reason?
            }
            if (!dontAddRoles) Subclass.Instance.server.MaybeAddRoles(p);
        }

        public static void AddToFF(Player p)
        {
            if (!FriendlyFired.Contains(p)) FriendlyFired.Add(p);
        }

        public static void TryToRemoveFromFF(Player p)
        {
            if (FriendlyFired.Contains(p)) FriendlyFired.Remove(p);
        }

        public static void AddCooldown(Player p, AbilityType ability)
        {
            if (!Cooldowns.ContainsKey(p)) Cooldowns.Add(p, new Dictionary<AbilityType, float>());
            Cooldowns[p][ability] = Time.time;
        }

        public static bool OnCooldown(Player p, AbilityType ability, SubClass subClass)
        {
            return Cooldowns.ContainsKey(p) && Cooldowns[p].ContainsKey(ability)
                && Time.time - Cooldowns[p][ability] < subClass.AbilityCooldowns[ability];
        }

        public static float TimeLeftOnCooldown(Player p, AbilityType ability, SubClass subClass, float time)
        {
            if (Cooldowns.ContainsKey(p) && Cooldowns[p].ContainsKey(ability))
            {
                return subClass.AbilityCooldowns[ability] - (time - Cooldowns[p][ability]);
            }
            return 0;
        }

        public static bool PlayerJustBypassedTeslaGate(Player p)
        {
            return PlayersThatBypassedTeslaGates.ContainsKey(p) && Time.time - PlayersThatBypassedTeslaGates[p] < 3f;
        }

        public static bool RoundJustStarted()
        {
            return Time.time - RoundStartedAt < 5f;
        }

        public static void AddPreviousTeam(Player p)
        {
            if (PreviousRoles.ContainsKey(p)) PreviousRoles[p] = p.Role;
            else PreviousRoles.Add(p, p.Role);
        }

        public static Nullable<RoleType> GetPreviousRole(Player p)
        {
            if (PreviousRoles.ContainsKey(p)) return PreviousRoles[p];
            return null;
        }

        public static Nullable<Team> GetPreviousTeam(Player p)
        {
            if (PreviousRoles.ContainsKey(p)) return PreviousRoles[p].GetTeam();
            return null;
        }
    }
}
