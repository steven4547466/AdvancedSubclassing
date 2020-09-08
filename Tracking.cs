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

        public static List<Player> FriendlyFired = new List<Player>();


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
            return subClass.AbilityCooldowns[ability] - (time - Cooldowns[p][ability]);
        }
    }
}
