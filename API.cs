using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass
{
    public static class API
    {

        public static bool GiveClass(Player p, SubClass subClass)
        {
            if (PlayerHasSubClass(p) || !subClass.AffectsRoles.Contains(p.Role)) return false;
            Tracking.AddClass(p, subClass);
            return true;
        }

        public static bool RemoveClass(Player p)
        {
            if (!PlayerHasSubClass(p)) return false;
            Tracking.RemoveAndAddRoles(p, true);
            return true;
        }

        public static Dictionary<string, SubClass> GetClasses()
        {
            return Subclass.Instance.Classes.ToDictionary(x => x.Key, x => x.Value);
        }

        public static Dictionary<Player, SubClass> GetPlayersWithSubclasses()
        {
            return Tracking.PlayersWithSubclasses.ToDictionary(x => x.Key, x => x.Value);
        }

        public static bool PlayerHasSubClass(Player p)
        {
            return Tracking.PlayersWithSubclasses.ContainsKey(p);
        }

        public static bool PlayerHasZombies(Player p)
        {
            return Tracking.PlayersWithZombies.ContainsKey(p);
        }

        public static bool PlayerHadZombies(Player p)
        {
            return Tracking.PlayersThatHadZombies.ContainsKey(p);
        }

        public static List<Player> PlayersZombies(Player p)
        {
            if (PlayerHasZombies(p)) return new List<Player>(Tracking.PlayersWithZombies[p].ToArray());
            return null;
        }

        public static List<Player> PlayersZombiesOld(Player p)
        {
            if (PlayerHadZombies(p)) return new List<Player>(Tracking.PlayersThatHadZombies[p].ToArray());
            return null;
        }

        public static List<Player> RevivedZombies()
        {
            List<Player> zombies = Tracking.PlayersWithZombies.SelectMany(e => e.Value).ToList();
            zombies.AddRange(Tracking.PlayersThatHadZombies.SelectMany(e => e.Value));
            return zombies;
        }

        public static bool? AbilityOnCooldown(Player p, AbilityType ability)
        {
            if (!PlayerHasSubClass(p)) return null;
            return Tracking.OnCooldown(p, ability, Tracking.PlayersWithSubclasses[p]);
        }

        public static float? TimeLeftOnCooldown(Player p, AbilityType ability)
        {
            if (!PlayerHasSubClass(p)) return null;
            return Tracking.TimeLeftOnCooldown(p, ability, Tracking.PlayersWithSubclasses[p], Time.time);
        }
    }
}
