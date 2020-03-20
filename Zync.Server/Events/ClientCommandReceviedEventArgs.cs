using System;

namespace Zync.Server.Events {
	public class ClientCommandReceviedEventArgs {
		public DateTime ReceviedTime { get; set; }
		public CommandBase CommandBase { get; set; }

		public ClientCommandReceviedEventArgs(DateTime dt, CommandBase _cmdBase) {
			ReceviedTime = dt;
			CommandBase = _cmdBase;
		}
	}
}
