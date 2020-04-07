using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Zync.Parent {
	internal class ClientConfig {
		internal string Uid;
		internal string IpAddress;
		internal Enums.CLIENT_STATE CurrentState;
		internal EndPoint ClientEndpoint;
		internal bool ShouldDisconnectConnection;
		internal readonly TcpClient ClientConnection;		

		internal ClientConfig(TcpClient _clientSocket) {
			ClientConnection = _clientSocket;
			Uid = string.Empty;
			IpAddress = string.Empty;
			CurrentState = Enums.CLIENT_STATE.CONNECTING;
			ClientEndpoint = null;
			ShouldDisconnectConnection = false;
		}

		internal void DisconnectClient() => ShouldDisconnectConnection = true;
	}
}
