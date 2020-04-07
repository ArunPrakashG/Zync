using FluentScheduler;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zync.Logging;
using Zync.Logging.Interfaces;
using Zync.Parent.Events;
using static Zync.Logging.Enums;

namespace Zync.Parent {
	internal class Processor {
		private readonly ILogger Logger;
		private Command PreviousCommand;
		private readonly ClientConfig Config;
		private readonly SemaphoreSlim ReceiveSync = new SemaphoreSlim(1, 1);

		internal delegate void OnReceivedDelegate(object sender, OnReceivedEventArgs e);
		internal delegate void OnDisconnectedDelegate(object sender, OnDisconnectedEventArgs e);
		internal delegate void OnRequestReceivedDelegate(object sender, OnRequestReceivedEventArgs e);
		internal event OnReceivedDelegate OnReceived;
		internal event OnDisconnectedDelegate OnDisconnected;
		internal event OnRequestReceivedDelegate OnRequest;

		internal Processor(TcpClient _client) {
			Config = new ClientConfig(_client ?? throw new ArgumentNullException(nameof(_client), "Socket cannot be null!")) {
				ClientEndpoint = _client.Client.RemoteEndPoint,
				IpAddress = _client.Client.RemoteEndPoint?.ToString()?.Split(':')[0].Trim()
			};

			Config.Uid = !string.IsNullOrEmpty(Config.IpAddress)
				? GenerateUniqueId(Config.IpAddress)
				: GenerateUniqueId(_client.GetHashCode().ToString());
			Logger = new Logger($"{nameof(Processor)} | {Config.Uid}");
		}

		internal ClientConfig GetConfiguration() => Config;

		private void SetState(Enums.CLIENT_STATE state) => Config.CurrentState = state;

		internal async Task Init() {
			Logger.Info($"Connected to client => {Config.IpAddress}");
			SetState(Enums.CLIENT_STATE.CONNECTED);
			ZyncParent.AddClient(this);
			await KeepAliveLoop().ConfigureAwait(false);
		}

		private async Task KeepAliveLoop() {
			Logger.Info("Client is ready. (R/S available)");
			SetState(Enums.CLIENT_STATE.READY);

			await ReceiveSync.WaitAsync().ConfigureAwait(false);
			try {
				while (!Config.ShouldDisconnectConnection) {
					if (Config.ClientConnection.Available <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
					}

					if (!Config.ClientConnection.Connected) {
						await DisconnectClientAsync(true).ConfigureAwait(false);
					}

					try {
						byte[] buffer = new byte[1024];
						int bytesRead;
						using FileStream output = File.Create("result.dat");
						using NetworkStream clientStream = Config.ClientConnection.GetStream();

						SetState(Enums.CLIENT_STATE.CONNECTED);
						Logger.Info("Client connected. Starting to receive the file...");

						while ((bytesRead = clientStream.Read(buffer, 0, buffer.Length)) > 0) {
							output.Write(buffer, 0, bytesRead);
						}

						string receviedMessage = Encoding.ASCII.GetString(buffer);
						receviedMessage = Regex.Replace(receviedMessage, "\\0", string.Empty);

						if (string.IsNullOrEmpty(receviedMessage)) {
							await Task.Delay(1).ConfigureAwait(false);
							continue;
						}

						Command cmdObject = new Command(receviedMessage);

						if (cmdObject.Equals(PreviousCommand)) {
							continue;
						}

						OnReceived?.Invoke(this, new OnReceivedEventArgs(receviedMessage));

						await OnRecevied(cmdObject).ConfigureAwait(false);
						PreviousCommand = cmdObject;
						await Task.Delay(1).ConfigureAwait(false);
					}
					catch (SocketException) {
						//this means client was forcefully disconnected from the remote endpoint. so process it here and disconnect the client and dispose
						await DisconnectClientAsync().ConfigureAwait(false);
						break;
					}
					catch (ThreadAbortException) {
						//This means the client is disconnected and the thread is aborted.
						break;
					}
					catch (Exception e) {
						Logger.Log(e);
						continue;
					}
				}
			}
			finally {
				ReceiveSync.Release();
				// Handle when keep alive loop ends/fails
			}			
		}		

		private async Task SendFileAsync() {

		}

		private async Task ReceiveFileAsync() {

		}

		public async Task SendCommandAsync(string? response) {
			if (string.IsNullOrEmpty(response)) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				await DisconnectClientAsync().ConfigureAwait(false);
				return;
			}

			Config.ClientConnection.Send(Encoding.ASCII.GetBytes(response));
		}

		internal static string GenerateUniqueId(string ipAddress) {
			if (string.IsNullOrEmpty(ipAddress)) {
				return string.Empty;
			}

			return ipAddress.ToLowerInvariant().Trim().GetHashCode().ToString();
		}

		public async Task DisconnectClientAsync(bool dispose = false) {
			Config.DisconnectClient();

			if (Config.ClientConnection.Connected) {
				Config.ClientConnection.Disconnect(true);
			}

			while (Config.ClientConnection.Connected) {
				Logger.Log("Waiting for client to disconnect...");
				await Task.Delay(5).ConfigureAwait(false);
			}

			Logger.Log($"Disconnected client => {Config.IpAddress}");

			OnDisconnected?.Invoke(this, new OnDisconnectedEventArgs(Config.IpAddress, Config.Uid, 5000));

			if (dispose) {
				Config.ClientConnection?.Close();
				Config.ClientConnection?.Dispose();

				JobManager.AddJob(() => {
					TCPServerCore.RemoveClient(this);
				}, TimeSpan.FromSeconds(5));
			}
		}
	}
}
