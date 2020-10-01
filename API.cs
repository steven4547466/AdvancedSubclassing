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

        public static bool GiveClass(Player p, SubClass subClass, bool lite = false)
        {
            if (PlayerHasSubClass(p) || !subClass.AffectsRoles.Contains(p.Role)) return false;
            Tracking.AddClass(p, subClass, Subclass.Instance.Scp035Enabled && scp035.API.Scp035Data.GetScp035().Id == p.Id, lite);
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
            return Subclass.Instance.Classes.ToDictionary(x => x.Key, x => new SubClass(x.Value));
        }

        public static void EnableAllClasses()
        {
            foreach (var subClass in Subclass.Instance.Classes) subClass.Value.BoolOptions["Enabled"] = true;
        }

        public static void DisableAllClasses()
        {
            foreach (var subClass in Subclass.Instance.Classes) subClass.Value.BoolOptions["Enabled"] = false;
        }

        public static bool EnableClass(SubClass sc)
        {
            try
            {
                Subclass.Instance.Classes[sc.Name].BoolOptions["Enabled"] = true;
                return true;
            }catch
            {
                return false;
            }
        }

        public static bool EnableClass(string sc)
        {
            try
            {
                Subclass.Instance.Classes[sc].BoolOptions["Enabled"] = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DisableClass(SubClass sc)
        {
            try
            {
                Subclass.Instance.Classes[sc.Name].BoolOptions["Enabled"] = false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DisableClass(string sc)
        {
            try
            {
                Subclass.Instance.Classes[sc].BoolOptions["Enabled"] = false;
                return true;
            }
            catch
            {
                return false;
            }
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
