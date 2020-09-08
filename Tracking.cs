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

        public static List<Player> FriendlyFired = new List<Player>();

        public static float RoundStartedAt = 0f;


        public static void RemoveAndAddRoles(Player p, bool dontAddRoles = false)
        {
            if (RoundJustStarted()) return;
            if (PlayersWithSubclasses.ContainsKey(p)) PlayersWithSubclasses.Remove(p);
            if (Cooldowns.ContainsKey(p)) Cooldowns.Remove(p);
            if (FriendlyFired.Contains(p)) FriendlyFired.RemoveAll(e => e == p);

            if (p.GameObject.GetComponent<MonoBehaviours.InfiniteSprint>() != null)
            {
                Log.Debug($"Player {p.Nickname} has infinite stamina, destroying", Subclass.Instance.Config.Debug);
                p.GameObject.GetComponent<MonoBehaviours.InfiniteSprint>().Destroy();
                Log.Info(p.GameObject.GetComponent<MonoBehaviours.InfiniteSprint>() == null);
                p.IsUsingStamina = true;
            }
            Log.Info(p.GameObject.GetComponent<MonoBehaviours.InfiniteSprint>() == null);
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
    }
}
