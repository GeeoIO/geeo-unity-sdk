using UnityEngine;

namespace GeeoSdk
{
	// Enum of available cumulative logging levels
	public enum LogLevel { None, Error, Warning, Verbose }

	/// <summary>
	/// Methods to handle console logs.
	/// </summary>
	internal static class DebugLogs
	{
		#region Logs Handling
		/// <summary>
		/// Log an error message to console.
		/// </summary>
		/// <param name="message">Message to log.</param>
		/// <param name="context">The involved object reference. (optional)</param>
		internal static void LogError(object message, Object context = null)
		{
			if (Geeo.Instance.logLevel >= LogLevel.Error)
				Debug.LogError(message, context);
		}

		/// <summary>
		/// Log a warning message to console.
		/// </summary>
		/// <param name="message">Message to log.</param>
		/// <param name="context">The involved object reference. (optional)</param>
		internal static void LogWarning(object message, Object context = null)
		{
			if (Geeo.Instance.logLevel >= LogLevel.Warning)
				Debug.LogWarning(message, context);
		}

		/// <summary>
		/// Log a verbose message to console.
		/// </summary>
		/// <param name="message">Message to log.</param>
		/// <param name="context">The involved object reference. (optional)</param>
		internal static void LogVerbose(object message, Object context = null)
		{
			if (Geeo.Instance.logLevel >= LogLevel.Verbose)
				Debug.Log(message, context);
		}
		#endregion
	}
}
