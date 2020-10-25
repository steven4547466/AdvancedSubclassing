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
    public class Aura : PlayerEffect
    {

        public float HealthPerTick = 5f;

		public float Radius = 4f;

        public Aura(ReferenceHub hub, float healthPerTick = 5f, float radius = 4f)
        {
			Hub = hub;
            TimeBetweenTicks = 5f;
            TimeLeft = 5f;

			HealthPerTick = healthPerTick;
			Radius = radius;
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
					IEnumerable<Player> players = Physics.OverlapSphere(Hub.transform.position, Radius).Where(t => Player.Get(t.gameObject) != null).Select(t => Player.Get(t.gameObject));
					foreach (Player p in players)
                    {
						//if ()
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
