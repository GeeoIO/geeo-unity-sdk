using UnityEngine;

namespace GeeoSdk
{
	/// <summary>
	/// Root class of the SDK. It is stateless and allows to perform basic operations with the SDK.
	/// </summary>
	public sealed class Geeo : MonoSingleton<Geeo>
	{
		// Geeo SDK's debug messages logging level
		[SerializeField] private LogLevel logLevel = LogLevel.Verbose;

		// The HTTP and WebSocket endpoint URLs
		[SerializeField] private string serverHttpUrl = "https://demo.geeo.io";
		[SerializeField] private string serverWsUrl = "wss://demo.geeo.io/ws";

		// If the WebSocket connection should be closed when the application focus is lost
		[SerializeField] private bool wsDisconnectOnApplicationPause = true;

		#region Public
		// The HTTP and WebSocket networking modules
		public GeeoHTTP http;
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
	}
}
