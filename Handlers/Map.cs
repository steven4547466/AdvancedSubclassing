using Exiled.API.Features;
using Exiled.Events.EventArgs;
using System.Collections.Generic;
using System.Linq;
using EPlayer = Exiled.API.Features.Player;

namespace Subclass.Handlers
{
    public class Map
    {
        public void OnExplodingGrenade(ExplodingGrenadeEventArgs ev)
        {
            if (!Tracking.PlayersWithSubclasses.ContainsKey(ev.Thrower) ||
                (!Tracking.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFlash) &&
                 !Tracking.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFrag)))
            {
                Log.Debug($"Player with name {ev.Thrower.Nickname} has no subclass", Subclass.Instance.Config.Debug);
                return;
            }
            if(Tracking.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFlash) && !ev.IsFrag)
            {
                ev.IsAllowed = false;
                UpdateHealths(ev, "HealGrenadeFlashHealAmount");
            }else if(Tracking.PlayersWithSubclasses[ev.Thrower].Abilities.Contains(AbilityType.HealGrenadeFrag) && ev.IsFrag)
            {
                ev.IsAllowed = false;
                UpdateHealths(ev, "HealGrenadeFragHealAmount");
            }
        }

        public void UpdateHealths(ExplodingGrenadeEventArgs ev, string type)
        {
            UnityEngine.Collider[] colliders = UnityEngine.Physics.OverlapSphere(ev.Grenade.transform.position, 4);
            foreach (UnityEngine.Collider collider in colliders.Where(c => c.name == "Player"))
            {
                EPlayer player = EPlayer.Get(collider.gameObject);
                if (player != null && player.Team == ev.Thrower.Team)
                {
                    try
                    {
                        player.Health += Tracking.PlayersWithSubclasses[ev.Thrower].FloatOptions[type];
                    }
                    catch(KeyNotFoundException e)
                    {
                        player.Health = player.MaxHealth;
                    }
                }
            }
        }

        public void UpdateHealths(UnityEngine.Collider[] colliders, EPlayer thrower, string type)
        {
            foreach (UnityEngine.Collider collider in colliders.Where(c => c.name == "Player"))
            {
                EPlayer player = EPlayer.Get(collider.gameObject);
                if (player != null && player.Team == thrower.Team)
                {
                    try
                    {
                        player.Health += Tracking.PlayersWithSubclasses[thrower].FloatOptions[type];
                    }
                    catch (KeyNotFoundException e)
                    {
                        player.Health = player.MaxHealth;
                    }
                }
            }
        }
    }
}
