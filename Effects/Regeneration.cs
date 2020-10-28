using CustomPlayerEffects;
using Exiled.API.Features;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.MonoBehaviours
{
    public class Regeneration : PlayerEffect
    {
        private Player player;

        public float HealthPerTick = 2f;
        public float ActiveAt = 0f;

        public Regeneration(ReferenceHub hub, float healthPerTick = 2f, float tickRate = 5f)
        {
            player = Player.Get(hub);

            Hub = hub;
            TimeBetweenTicks = tickRate;
            TimeLeft = tickRate;

            HealthPerTick = healthPerTick;
        }

        public override void PublicUpdate()
        {
            if (!Enabled || Time.time < ActiveAt)
            {
                TimeLeft = TimeBetweenTicks;
                return;
            }

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
                    if (player.Health + HealthPerTick < player.MaxHealth) player.Health += HealthPerTick;
                    else player.Health = player.MaxHealth;
                }
            }
            else
            {
                TimeLeft = TimeBetweenTicks;
            }
        }
    }
}
