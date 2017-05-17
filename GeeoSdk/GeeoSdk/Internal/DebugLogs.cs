using UnityEngine;

namespace GeeoSdk
{
	/// <summary>
	/// Enum of available cumulative logging levels.
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// Log no message.
		/// </summary>
		None,

		/// <summary>
		/// Log only error messages.
		/// </summary>
		Error,

		/// <summary>
		/// Log only error and warning messages.
		/// </summary>
		Warning,

		/// <summary>
		/// Log all messages (error, warning, verbose).
		/// </summary>
		Verbose
	}

	/// <summary>
	/// Methods to handle console logs.
	/// </summary>
	internal static class DebugLogs
	{
		#region Logs Handling
		// Current logging level allowed (not allowed logs won't display)
		public static LogLevel logLevel = LogLevel.Verbose;

		/// <summary>
		/// Log an error message to console.
		/// </summary>
		/// <param name="message">Message to log.</param>
		/// <param name="context">The involved object reference. (optional)</param>
		public static void LogError(object message, Object context = null)
		{
			if (logLevel >= LogLevel.Error)
				Debug.LogError(message, context);
		}

		/// <summary>
		/// Log a warning message to console.
		/// </summary>
		/// <param name="message">Message to log.</param>
		/// <param name="context">The involved object reference. (optional)</param>
		public static void LogWarning(object message, Object context = null)
		{
			if (logLevel >= LogLevel.Warning)
				Debug.LogWarning(message, context);
		}

		/// <summary>
		/// Log a verbose message to console.
		/// </summary>
		/// <param name="message">Message to log.</param>
		/// <param name="context">The involved object reference. (optional)</param>
		public static void LogVerbose(object message, Object context = null)
		{
			if (logLevel >= LogLevel.Verbose)
				Debug.Log(message, context);
		}
		#endregion
	}
}
