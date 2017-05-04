namespace GeeoSdk
{
	/// <summary>
	/// The SDK's WebSocket module to manage a Websocket connection to a Geeo instance.
	/// </summary>
	public sealed class GeeoWS
	{
		// The WebSocket endpoint URL
		private string wsUrl;

		/// <summary>
		/// WebSocket module's constructor.
		/// </summary>
		/// <param name="httpUrl">WebSocket endpoint URL.</param>
		public GeeoWS(string _wsUrl)
		{
			wsUrl = _wsUrl;
		}
	}
}
