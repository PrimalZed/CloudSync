using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PrimalZed.CloudSync.Logging {
	internal class EventLogRegistrar(ILogger<EventLogRegistrar> logger) {
		private const string EVENTLOG_SOURCE = "CloudSync.Console";
		private const string EVENTLOG_LOGNAME = "Application";

		public void Register() {
			logger.LogDebug("Create event log source if it does not already exist");
			if (!EventLog.SourceExists(EVENTLOG_SOURCE)) {
				EventLog.CreateEventSource(EVENTLOG_SOURCE, EVENTLOG_LOGNAME);
			}
		}

		public void Unregister() {
			logger.LogDebug("Remove event log source if it exists");
			if (EventLog.SourceExists(EVENTLOG_SOURCE)) {
				EventLog.DeleteEventSource(EVENTLOG_SOURCE);
			}
		}
	}
}
