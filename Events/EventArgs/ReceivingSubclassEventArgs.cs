using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;

namespace Subclass.Events.EventArgs
{
	public class ReceivingSubclassEventArgs : System.EventArgs
	{
		public ReceivingSubclassEventArgs(Player player, SubClass subClass, bool isAllowed = true)
		{
			Player = player;
			Subclass = subClass;
			IsAllowed = isAllowed;
		}

		public Player Player { get; }

		public SubClass Subclass { get; set; }

		public bool IsAllowed { get; set; }
	}
}
