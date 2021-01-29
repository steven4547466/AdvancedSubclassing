using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Exiled.Events.Events;
using Subclass.Events.EventArgs;
using Exiled.Events.Extensions;

namespace Subclass.Events.Handlers
{
	public static class Player
	{
		public static event CustomEventHandler<ReceivingSubclassEventArgs> ReceivingSubclass;

		public static event CustomEventHandler<ReceivedSubclassEventArgs> ReceivedSubclass;

		public static void OnReceivingSubclass(ReceivingSubclassEventArgs ev) => ReceivingSubclass.InvokeSafely(ev);

		public static void OnReceivedSubclass(ReceivedSubclassEventArgs ev) => ReceivedSubclass.InvokeSafely(ev);
	}
}
