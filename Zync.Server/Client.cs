using FluentScheduler;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zync.Logging;
using Zync.Logging.Interfaces;
using Zync.Server.Events;
using static Zync.Logging.Enums;

namespace Zync.Server {
	internal class Client {
		private readonly ILogger Logger;
		private Command PreviousCommand;
		private readonly ClientConfig Config;
		private readonly SemaphoreSlim ReceiveSync = new SemaphoreSlim(1, 1);

		internal delegate void OnReceivedDelegate(object sender, ClientMessageEventArgs e);
		internal delegate void OnDisconnectedDelegate(object sender, ClientDisconnectedEventArgs e);
		internal delegate void OnCommandReceivedDelegate(object sender, ClientCommandReceviedEventArgs e);
		internal event OnReceivedDelegate OnReceived;
		internal event OnDisconnectedDelegate OnDisconnected;
		internal event OnCommandReceivedDelegate OnCommandRecevied;

		internal Client(Socket sock) {
			Config = new ClientConfig(sock ?? throw new ArgumentNullException(nameof(sock), "Socket cannot be null!")) {
				ClientEndpoint = sock.RemoteEndPoint,
				IpAddress = sock.RemoteEndPoint?.ToString()?.Split(':')[0].Trim()
			};

			Config.Uid = !string.IsNullOrEmpty(Config.IpAddress)
				? GenerateUniqueId(Config.IpAddress)
				: GenerateUniqueId(sock.GetHashCode().ToString());			
			Logger = new Logger($"{nameof(Client)} | {Config.Uid}");
		}

		internal ClientConfig GetConfiguration() => Config;

		private void SetState(Enums.CLIENT_STATE state) => Config.CurrentState = state;

		internal async Task Init() {
			Logger.Info($"Connected to client => {Config.IpAddress}");
			SetState(Enums.CLIENT_STATE.CONNECTED);
			ZyncServer.AddClient(this);
			await RecevieAsync().ConfigureAwait(false);
		}

		private async Task RecevieAsync() {
			Logger.Info("Client is ready. (R/S available)");
			SetState(Enums.CLIENT_STATE.READY);

			do {
				if (Config.ClientSocket.Available <= 0) {
					await Task.Delay(1).ConfigureAwait(false);
					continue;
				}

				if (!Config.ClientSocket.Connected) {
					await DisconnectClientAsync(true).ConfigureAwait(false);
				}

				await ReceiveSync.WaitAsync().ConfigureAwait(false);

				try {
					byte[] buffer = new byte[5024];
					int i = Config.ClientSocket.Receive(buffer);

					if (i <= 0) {
						await Task.Delay(1).ConfigureAwait(false);
						continue;
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

					OnReceived?.Invoke(this, new ClientMessageEventArgs(receviedMessage));

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
				finally {
					ReceiveSync.Release();
				}
			} while (!Config.ShouldDisconnectConnection);
		}

		public async Task SendResponseAsync(string? response) {
			if (string.IsNullOrEmpty(response)) {
				return;
			}

			if (!Helpers.IsSocketConnected(ClientSocket)) {
				Logger.Log("Failed to send response as client is disconnected.", LogLevels.Warn);
				await DisconnectClientAsync().ConfigureAwait(false);
				return;
			}

			Config.ClientSocket.Send(Encoding.ASCII.GetBytes(response));
		}

		internal static string GenerateUniqueId(string ipAddress) {
			if (string.IsNullOrEmpty(ipAddress)) {
				return string.Empty;
			}

			return ipAddress.ToLowerInvariant().Trim().GetHashCode().ToString();
		}

		public async Task DisconnectClientAsync(bool dispose = false) {
			Config.DisconnectClient();

			if (Config.ClientSocket.Connected) {
				Config.ClientSocket.Disconnect(true);
			}

			while (Config.ClientSocket.Connected) {
				Logger.Log("Waiting for client to disconnect...");
				await Task.Delay(5).ConfigureAwait(false);
			}

			Logger.Log($"Disconnected client => {Config.IpAddress}");

			OnDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(Config.IpAddress, Config.Uid, 5000));

			if (dispose) {
				Config.ClientSocket?.Close();
				Config.ClientSocket?.Dispose();

				JobManager.AddJob(() => {
					TCPServerCore.RemoveClient(this);
				}, TimeSpan.FromSeconds(5));
			}
		}
	}
}
