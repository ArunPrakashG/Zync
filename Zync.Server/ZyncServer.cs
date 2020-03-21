namespace Zync.Server {
	using FluentScheduler;
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading;
	using System.Threading.Tasks;
	using Zync.Logging;
	using Zync.Logging.Interfaces;
	using static Zync.Logging.Enums;

	public class ZyncServer {
		private static readonly ILogger Logger = new Logger(nameof(ZyncServer));
		private readonly TcpListener Listener;
		private readonly int ServerPort;
		private readonly IPAddress ListerningAddress;
		private bool IsStopRequested;
		private readonly SemaphoreSlim ListSemaphore = new SemaphoreSlim(1, 1);
		private readonly List<Client> Clients = new List<Client>();
		private bool IsOnline;

		static ZyncServer() => JobManager.Initialize(new Registry());

		public ZyncServer(IPAddress _address, int _port) {
			if(_port <= 0) {
				throw new ArgumentOutOfRangeException(nameof(_port));
			}

			if(_address == null) {
				throw new ArgumentNullException(nameof(_address));
			}

			ServerPort = _port;
			ListerningAddress = _address;
			Listener = new TcpListener(ListerningAddress, ServerPort);
			
		}

		public void InitServer() {
			if (IsOnline) {
				return;
			}

			Logger.Log("Starting Zync Server...", LogLevels.Trace);			
			Listener.Start(10);
			IsOnline = true;

			Logger.Log("Zync Server listening for connections...");
			Helpers.InBackgroundThread(async () => {
				do {
					if (Listener.Pending()) {
						Socket socket = await Listener.AcceptSocketAsync().ConfigureAwait(false);
						Client client = new Client(socket);
						Helpers.InBackground(client.Init, true);
					}

					await Task.Delay(1).ConfigureAwait(false);
				}
				while (!IsStopRequested && Listener != null);
			}, "AlwaysOn Server thread", true);
		}

		private async Task ShutdownServer() {
			if (Clients.Count > 0) {
				foreach (Client client in Clients) {
					if (client == null) {
						continue;
					}

					await client.DisconnectClientAsync(true).ConfigureAwait(false);
				}
			}
			Listener.Stop();
			IsOnline = false;
			Logger.Log("Zync Server stopped.");
		}

		public static void AddClient(Client client) {
			if (client == null) {
				return;
			}

			try {
				ListSemaphore.Wait();

				if (Clients.Any(x => x.UniqueId != null && x.UniqueId.Equals(client.UniqueId))) {
					return;
				}

				if (!Clients.Contains(client)) {
					Clients.Add(client);
					Logger.Log("Added to client collection");
				}
			}
			finally {
				ListSemaphore.Release();
			}
		}

		public static void RemoveClient(Client client) {
			if (client == null) {
				return;
			}

			try {
				ListSemaphore.Wait();
				if (!Clients.Any(x => x.UniqueId != null && x.UniqueId.Equals(client.UniqueId))) {
					return;
				}

				if (Clients.Contains(client)) {
					Clients.Remove(client);
					Logger.Log("Removed from client collection");
				}
			}
			finally {
				ListSemaphore.Release();
			}
		}
	}
}
