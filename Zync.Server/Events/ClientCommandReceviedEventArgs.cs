using System;

namespace Zync.Parent.Events {
	public class OnRequestReceivedEventArgs {
		public DateTime ReceviedTime { get; set; }
		public CommandBase CommandBase { get; set; }

		public OnRequestReceivedEventArgs(DateTime dt, CommandBase _cmdBase) {
			ReceviedTime = dt;
			CommandBase = _cmdBase;
		}
	}
}
