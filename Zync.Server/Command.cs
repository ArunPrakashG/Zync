using System;

namespace Zync.Server {
	internal struct Command {
		internal readonly string CommandString;
		internal readonly DateTime ReceivedAt;

		internal Command(string _command) {
			CommandString = _command;
			ReceivedAt = DateTime.Now;
		}
	}
}
