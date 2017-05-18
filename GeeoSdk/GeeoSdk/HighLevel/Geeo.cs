using UnityEngine;

namespace GeeoSdk
{
	/// <summary>
	/// Root class of the SDK. It is stateless and allows to perform basic operations with the SDK.
	/// </summary>
	public sealed class Geeo : MonoSingleton<Geeo>
	{
		/// <summary>
		/// Current Geeo SDK's version.
		/// </summary>
		public const string sdkVersion = "0.1";

		// Geeo SDK's debug messages logging level
		[SerializeField] private LogLevel logLevel = LogLevel.Verbose;

		// The HTTP and WebSocket endpoint URLs
		[SerializeField] private string serverHttpUrl = "https://demo.geeo.io";
		[SerializeField] private string serverWsUrl = "wss://demo.geeo.io/ws";

		// If the WebSocket connection should be closed when the application focus is lost
		[SerializeField] private bool wsDisconnectOnApplicationPause = true;

		#region Public
		/// <summary>
		/// The Geeo SDK's HTTP networking module instance.
		/// </summary>
		public GeeoHTTP http;

		/// <summary>
		/// The Geeo SDK's WebSocket networking module instance.
		/// </summary>
		public GeeoWS ws;
		#endregion

		#region Initializations
		/// <summary>
		/// Initialize SDK's debug messages logging level and networking modules at Awake.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();

			// Set the logging level
			DebugLogs.logLevel = logLevel;

			// Instantiate networking modules
			http = new GeeoHTTP(serverHttpUrl);
			ws = new GeeoWS(serverWsUrl, wsDisconnectOnApplicationPause);
		}
		#endregion

		#region MonoBehaviour Events
		/// <summary>
		/// Transmit the OnApplicationPause MonoBehaviour event to the WebSocket module.
		/// </summary>
		/// <param name="paused">If the application lost the focus.</param>
		private void OnApplicationPause(bool paused)
		{
			ws.OnApplicationPause(paused);
		}

		/// <summary>
		/// Transmit the OnApplicationQuit MonoBehaviour event to the WebSocket module.
		/// </summary>
		private void OnApplicationQuit()
		{
			ws.OnApplicationQuit();
		}
		#endregion
	}
}
