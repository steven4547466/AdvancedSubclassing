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
        public bool SplitHealth = true;

        public float HealthPerTick = 5f;

        public Aura(ReferenceHub hub)
        {
			Hub = hub;
            TimeBetweenTicks = 5f;
            TimeLeft = 5f;
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
				}
			}
			else
			{
				TimeLeft = TimeBetweenTicks;
			}
		}
    }
}
