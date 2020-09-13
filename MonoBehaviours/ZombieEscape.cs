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
    class ZombieEscape : NetworkBehaviour
    {
        private Player player;
        public bool Enabled = true;

        private void Awake()
        {
            player = Player.Get(gameObject);
        }

        private void Update()
        {
            if (Enabled)
            {
                if (Vector3.Distance(base.transform.position, base.GetComponent<Escape>().worldPosition) < (Escape.radius))
                {
                    Player p = Player.Get(gameObject);
                    if (p != null && Tracking.PlayersThatHadZombies.Any(e => e.Value.Contains(p)))
                    {
                        var item = Tracking.PlayersThatHadZombies.First(e => e.Value.Contains(p));
                        p.SetRole(item.Key.Role, false, true);
                    }
                }
            }
        }

        public void Destroy()
        {
            Enabled = false;
            DestroyImmediate(this, true);
        }
    }
}
