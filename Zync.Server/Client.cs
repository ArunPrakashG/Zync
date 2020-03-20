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
		private Command PreviousCommand { get; set; }
		public string? UniqueId { get; private set; }
		public string? IpAddress { get; private set; }
		public Socket ClientSocket { get; set; }
		public bool DisconnectConnection { get; set; }
		public EndPoint? ClientEndPoint { get; set; }

		public delegate void OnClientMessageRecevied(object sender, ClientMessageEventArgs e);
		public delegate void OnClientDisconnected(object sender, ClientDisconnectedEventArgs e);
		public delegate void OnClientCommandRecevied(object sender, ClientCommandReceviedEventArgs e);
		public event OnClientMessageRecevied? OnMessageRecevied;
		public event OnClientDisconnected? OnDisconnected;
		public event OnClientCommandRecevied? OnCommandRecevied;

		public Client(Socket sock) {
			ClientSocket = sock ?? throw new ArgumentNullException(nameof(sock), "Socket cannot be null!");
			ClientEndPoint = ClientSocket.RemoteEndPoint;
			IpAddress = ClientEndPoint?.ToString()?.Split(':')[0].Trim();

			UniqueId = IpAddress != null && !string.IsNullOrEmpty(IpAddress)
				? GenerateUniqueId(IpAddress)
				: GenerateUniqueId(ClientSocket.GetHashCode().ToString());
			Logger = new Logger($"CLIENT | {UniqueId}");
		}

		public async Task Init() {
			Logger.Log($"Connected client IP => {IpAddress} / {UniqueId}", LogLevels.Info);
			ZyncServer.AddClient(this);
			await RecevieAsync().ConfigureAwait(false);
		}

		private async Task RecevieAsync() {
			do {
				if (ClientSocket.Available <= 0) {
					await Task.Delay(1).ConfigureAwait(false);
					continue;
				}

				if (!ClientSocket.Connected) {
					await DisconnectClientAsync(true).ConfigureAwait(false);
				}

				try {
					byte[] buffer = new byte[5024];
					int i = ClientSocket.Receive(buffer);

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

					CommandObject cmdObject = new CommandObject(receviedMessage, DateTime.Now);

					if (PreviousCommand != null && cmdObject.Equals(PreviousCommand)) {
						continue;
					}

					OnMessageRecevied?.Invoke(this, new ClientMessageEventArgs(receviedMessage));

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
			} while (!DisconnectConnection);
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

			ClientSocket.Send(Encoding.ASCII.GetBytes(response));
		}

		private static string GenerateUniqueId(string ipAddress) {
			if (string.IsNullOrEmpty(ipAddress)) {
				return string.Empty;
			}

			return ipAddress.ToLowerInvariant().Trim().GetHashCode().ToString();
		}

		private static string FormatResponse(CommandResponseCode responseCode, ResponseObjectType respType, string? msg = null, string? json = null) {
			ResponseBase response = new ResponseBase(responseCode, respType, msg, json);
			return response.AsJson();
		}
	}
}
