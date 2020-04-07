using System;

namespace Zync.Parent {
	internal struct Command {
		internal readonly int CommandUid;
		internal readonly string CommandString;
		internal readonly DateTime ReceivedAt;

		internal Command(string _command) {
			CommandString = _command;
			CommandUid = CommandString.GetHashCode();
			ReceivedAt = DateTime.Now;
		}

		public override bool Equals(object obj) {
			try {
				Command _cmd = (Command) obj;

				if (_cmd.CommandUid == CommandUid) {
					return true;
				}

				return false;
			}
			catch {
				return false;
			}
		}

		public override int GetHashCode() => base.GetHashCode();

		public override string ToString() => $"{CommandUid}|{CommandString}|{ReceivedAt}";
	}
}
