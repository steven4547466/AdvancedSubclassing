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
			if (PlayerHasSubclass(p) || !subClass.AffectsRoles.Contains(p.Role)) return false;
			if (Subclass.Instance.Scp035Enabled)
			{
				Player scp035 = (Player)Loader.Plugins.First(pl => pl.Name == "scp035").Assembly.GetType("scp035.API.Scp035Data").GetMethod("GetScp035", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
				TrackingAndMethods.AddClass(p, subClass, Subclass.Instance.Scp035Enabled && scp035?.Id == p.Id, lite);
				return true;
			}
			TrackingAndMethods.AddClass(p, subClass, false, lite);
			return true;
		}

		public static bool RemoveClass(Player p)
		{
			if (!PlayerHasSubclass(p)) return false;
			TrackingAndMethods.RemoveAndAddRoles(p, true);
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
			}
			catch
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
			return TrackingAndMethods.PlayersWithSubclasses.ToDictionary(x => x.Key, x => x.Value);
		}

		public static SubClass GetPlayersSubclass(Player p)
		{
			if (!PlayerHasSubclass(p)) return null;
			return new SubClass(TrackingAndMethods.PlayersWithSubclasses[p]);
		}

		public static bool PlayerHasSubclass(Player p)
		{
			return TrackingAndMethods.PlayersWithSubclasses.ContainsKey(p);
		}

		public static void PreventPlayerFromGettingClass(Player p, float duration)
		{
			if (!TrackingAndMethods.PlayersThatJustGotAClass.ContainsKey(p)) TrackingAndMethods.PlayersThatJustGotAClass.Add(p, Time.time + duration);
			else TrackingAndMethods.PlayersThatJustGotAClass[p] = Time.time + duration;
		}

		public static bool PlayerHasZombies(Player p)
		{
			return TrackingAndMethods.PlayersWithZombies.ContainsKey(p);
		}

		public static bool PlayerHadZombies(Player p)
		{
			return TrackingAndMethods.PlayersThatHadZombies.ContainsKey(p);
		}

		public static List<Player> PlayersZombies(Player p)
		{
			if (PlayerHasZombies(p)) return new List<Player>(TrackingAndMethods.PlayersWithZombies[p]);
			return null;
		}

		public static List<Player> PlayersZombiesOld(Player p)
		{
			if (PlayerHadZombies(p)) return new List<Player>(TrackingAndMethods.PlayersThatHadZombies[p]);
			return null;
		}

		public static List<Player> RevivedZombies()
		{
			List<Player> zombies = TrackingAndMethods.PlayersWithZombies.SelectMany(e => e.Value).ToList();
			zombies.AddRange(TrackingAndMethods.PlayersThatHadZombies.SelectMany(e => e.Value));
			return zombies;
		}

		public static bool AbilityOnCooldown(Player p, AbilityType ability)
		{
			if (!PlayerHasSubclass(p)) return false;
			return TrackingAndMethods.OnCooldown(p, ability, TrackingAndMethods.PlayersWithSubclasses[p]);
		}

		public static float TimeLeftOnCooldown(Player p, AbilityType ability)
		{
			if (!PlayerHasSubclass(p)) return 0;
			return TrackingAndMethods.TimeLeftOnCooldown(p, ability, TrackingAndMethods.PlayersWithSubclasses[p], Time.time);
		}

		public static void RegisterCustomWeaponGetter(MethodInfo findWeapon, MethodInfo weaponObtained)
		{
			TrackingAndMethods.CustomWeaponGetters.Add(new Tuple<MethodInfo, MethodInfo>(findWeapon, weaponObtained));
		}
	}
}
