using Exiled.API.Features;
using Exiled.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass
{

    /*
        I get this looks weird because I'm making lists and dictionaries out of lists and dictionaries,
        but I'm looking into ways to make it read only without creating new instaces of each thing.
     */
    public static class API
    {
        public static bool GiveClass(Player p, SubClass subClass, bool lite = false)
        {
            if (PlayerHasSubClass(p) || !subClass.AffectsRoles.Contains(p.Role)) return false;
            if (Subclass.Instance.Scp035Enabled) 
            {
                Player scp035 = (Player) Loader.Plugins[Loader.Plugins.FindIndex(pl => pl.Name == "scp035")].Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                Tracking.AddClass(p, subClass, Subclass.Instance.Scp035Enabled && scp035?.Id == p.Id, lite);
                return true;
            }
            Tracking.AddClass(p, subClass, false, lite);
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
            if (PlayerHasZombies(p)) return new List<Player>(Tracking.PlayersWithZombies[p]);
            return null;
        }

        public static List<Player> PlayersZombiesOld(Player p)
        {
            if (PlayerHadZombies(p)) return new List<Player>(Tracking.PlayersThatHadZombies[p]);
            return null;
        }

        public static List<Player> RevivedZombies()
        {
            List<Player> zombies = Tracking.PlayersWithZombies.SelectMany(e => e.Value).ToList();
            zombies.AddRange(Tracking.PlayersThatHadZombies.SelectMany(e => e.Value));
            return zombies;
        }

        public static bool AbilityOnCooldown(Player p, AbilityType ability)
        {
            if (!PlayerHasSubClass(p)) return false;
            return Tracking.OnCooldown(p, ability, Tracking.PlayersWithSubclasses[p]);
        }

        public static float TimeLeftOnCooldown(Player p, AbilityType ability)
        {
            if (!PlayerHasSubClass(p)) return 0;
            return Tracking.TimeLeftOnCooldown(p, ability, Tracking.PlayersWithSubclasses[p], Time.time);
        }
    }
}
