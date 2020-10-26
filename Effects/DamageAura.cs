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
	public class DamageAura : PlayerEffect
	{
		Player player = null;

		public float HealthPerTick = 5f;
		public float Radius = 4f;

		public bool AffectSelf = false;
		public bool AffectAllies = false;
		public bool AffectEnemies = true;

		public DamageAura(ReferenceHub hub, float healthPerTick = 5f, float radius = 4f, bool affectSelf = false, bool affectAllies = false, bool affectEnemies = true, float tickRate = 5f)
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
						p.ReferenceHub.playerStats.HurtPlayer(new PlayerStats.HitInfo(HealthPerTick, player.Nickname, DamageTypes.Poison, player.Id), p.GameObject);
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
