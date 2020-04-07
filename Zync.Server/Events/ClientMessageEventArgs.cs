namespace Zync.Parent.Events {
	public class OnReceivedEventArgs {
		public string RawCommandJson { get; set; } = string.Empty;

		public OnReceivedEventArgs(string command) => RawCommandJson = command;
	}
}
