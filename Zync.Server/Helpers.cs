using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zync.Server {
	internal class Helpers {

		public static Thread InBackgroundThread(Action action, string threadName, bool longRunning = false) {
			if (action == null) {
				return null;
			}

			ThreadStart threadStart = new ThreadStart(action);
			Thread BackgroundThread = new Thread(threadStart);

			if (longRunning) {
				BackgroundThread.IsBackground = true;
			}

			BackgroundThread.Name = !string.IsNullOrEmpty(threadName) ? threadName : action.GetHashCode().ToString();
			BackgroundThread.Priority = ThreadPriority.Normal;
			BackgroundThread.Start();
			return BackgroundThread;
		}

		public static bool IsNetworkAvailable() {
			try {
				Ping myPing = new Ping();
				string host = "8.8.8.8";
				byte[] buffer = new byte[32];
				int timeout = 1000;
				PingOptions pingOptions = new PingOptions();
				PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
				return reply != null && reply.Status == IPStatus.Success;
			}
			catch (Exception e) {				
				return false;
			}
		}

		public static Thread? InBackgroundThread(Action action, bool longRunning = false) {
			if (action == null) {				
				return null;
			}

			ThreadStart threadStart = new ThreadStart(action);
			Thread BackgroundThread = new Thread(threadStart);

			if (longRunning) {
				BackgroundThread.IsBackground = true;
			}

			BackgroundThread.Name = action.GetHashCode().ToString();
			BackgroundThread.Priority = ThreadPriority.Normal;
			BackgroundThread.Start();
			return BackgroundThread;
		}

		public static void InBackground(Action action, bool longRunning = false) {
			if (action == null) {				
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(action, CancellationToken.None, options, TaskScheduler.Default);
		}

		public static void InBackground<T>(Func<T> function, bool longRunning = false) {
			if (function == null) {				
				return;
			}

			TaskCreationOptions options = TaskCreationOptions.DenyChildAttach;

			if (longRunning) {
				options |= TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness;
			}

			Task.Factory.StartNew(function, CancellationToken.None, options, TaskScheduler.Default);
		}
	}
}
