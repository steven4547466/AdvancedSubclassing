using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CustomPlayerEffects;
using Exiled.API.Features;
using Mirror;
using UnityEngine;


namespace Subclass.Effects
{
    public class HealAura : PlayerEffect
    {
		Player player = null;

        public float HealthPerTick = 5f;
		public float Radius = 4f;

		public bool AffectSelf = true;
		public bool AffectAllies = true;
		public bool AffectEnemies = false;

        public HealAura(ReferenceHub hub, float healthPerTick = 5f, float radius = 4f, bool affectSelf = true, bool affectAllies = true, bool affectEnemies = false, float tickRate = 5f)
        {
			player = Player.Get(hub);

			Hub = hub;
            TimeBetweenTicks = tickRate;
            TimeLeft = tickRate;

			HealthPerTick = healthPerTick;
			Radius = radius;
			AffectSelf = affectSelf;
			AffectAllies = affectAllies;
			AffectEnemies = affectEnemies;
        }

        public override void PublicUpdate()
        {
			if (!NetworkServer.active)
			{
				return;
			}
			if (Enabled)
			{
				TimeLeft -= Time.deltaTime;
				if (TimeLeft <= 0f)
				{
					TimeLeft += TimeBetweenTicks;
					IEnumerable<Player> players = Physics.OverlapSphere(Hub.transform.position, Radius).Where(t => Player.Get(t.gameObject) != null).Select(t => Player.Get(t.gameObject)).Distinct();
					foreach (Player p in players)
                    {
						if ((!AffectEnemies && p.Team != player.Team) || (p.Id != player.Id && !AffectAllies && p.Team == player.Team)) continue;
						if (p.Id == player.Id && !AffectSelf) continue;
						if (p.Health == p.MaxHealth) continue;
						if (Tracking.PlayersWithSubclasses.ContainsKey(p) && Tracking.PlayersWithSubclasses[p].Abilities.Contains(AbilityType.CantHeal)) continue;
						if (p.Health + HealthPerTick < p.MaxHealth) p.Health += HealthPerTick;
						else p.Health = p.MaxHealth;
					}
				}
			}
			else
			{
				TimeLeft = TimeBetweenTicks;
			}
		}
    }
}
