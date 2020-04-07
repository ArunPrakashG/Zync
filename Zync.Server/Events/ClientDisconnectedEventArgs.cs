namespace Zync.Parent.Events {
	public class OnDisconnectedEventArgs {
		public string? ClientIp { get; set; }
		public string? UniqueId { get; set; }

		public double DisconnectDelay { get; set; }

		public OnDisconnectedEventArgs(string _clientIp, string _uniqueId, double _delay) {
			ClientIp = _clientIp;
			UniqueId = _uniqueId;
			DisconnectDelay = _delay;
		}
	}
}
