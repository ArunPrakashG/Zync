using System;
using System.Collections.Generic;
using System.Text;

namespace Zync.Server {
	internal class Enums {
		internal enum CLIENT_STATE {
			CONNECTING,
			CONNECTED,
			DISCONNECTING,
			DISCONNECTED,
			READY
		}
	}
}
