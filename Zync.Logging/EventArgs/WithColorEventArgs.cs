using Newtonsoft.Json;
using System;

namespace Zync.Logging.EventArgs {
	[Serializable]
	public class WithColorEventArgs {
		[JsonProperty]
		public string? LogIdentifier { get; private set; }

		[JsonProperty]
		public DateTime LogTime { get; private set; }

		[JsonProperty]
		public string? LogMessage { get; private set; }

		[JsonProperty]
		public string? CallerMemberName { get; private set; }

		[JsonProperty]
		public ConsoleColor Color { get; private set; }

		[JsonProperty]
		public int CallerLineNumber { get; private set; }

		[JsonProperty]
		public string? CallerFilePath { get; private set; }

		public WithColorEventArgs(string? logId, DateTime dt, string? msg, ConsoleColor color, string? callerName, int callerLine, string? callerFile) {
			LogIdentifier = logId;
			LogTime = dt;
			Color = color;
			LogMessage = msg;
			CallerMemberName = callerName;
			CallerLineNumber = callerLine;
			CallerFilePath = callerFile;
		}
	}
}
