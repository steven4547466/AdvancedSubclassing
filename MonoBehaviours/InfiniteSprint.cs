using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Subclass.MonoBehaviours
{
	public class InfiniteSprint : MonoBehaviour
	{
		private Player player;
		public bool Enabled = true;

		private void Awake()
		{
			player = Player.Get(gameObject);
		}

		private void Update()
		{
			if (Enabled) player.IsUsingStamina = false;
		}

		public void Destroy()
		{
			Enabled = false;
			DestroyImmediate(this, true);
		}
	}
}
