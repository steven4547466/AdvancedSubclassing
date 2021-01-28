using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exiled.API.Features;

namespace Subclass.Events.EventArgs
{
	public class ReceivedSubclassEventArgs : System.EventArgs
	{
		public ReceivedSubclassEventArgs(Player player, SubClass subClass)
		{
			Player = player;
			Subclass = subClass;
		}

		public Player Player { get; }

		public SubClass Subclass { get; }
	}
}
