using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeeoSdk
{
	/// <summary>
	/// The SDK's WebSocket module to manage a Websocket connection to a Geeo instance.
	/// </summary>
	public sealed class GeeoWS
	{
		#region Constructor
		// The WebSocket endpoint URL
		private string wsUrl;

		// If the WebSocket connection should be closed when the application focus is lost
		private bool disconnectOnApplicationPause;

		// The actual WebSocket
		private WebSocket webSocket;

		/// <summary>
		/// WebSocket module's constructor.
		/// </summary>
		/// <param name="_wsUrl">The WebSocket endpoint URL.</param>
		/// <param name="_disconnectOnApplicationPause">If the WebSocket connection should be closed when the application focus is lost.</param>
		public GeeoWS(string _wsUrl, bool _disconnectOnApplicationPause = true)
		{
			wsUrl = _wsUrl;
			disconnectOnApplicationPause = _disconnectOnApplicationPause;

			// Instantiate a specific WebSocket implement depending on which platform the application is running on
			switch (Application.platform)
			{
				case RuntimePlatform.WebGLPlayer:
				webSocket = new WebSocket_WebGL();
				break;

				default:
				webSocket = new WebSocket_CrossPlatform();
				break;
			}
		}
		#endregion

		#region WebSocket Life Handling
		// The complete URL format for a "websocket connect" request
		private const string webSocketConnect_requestUrlFormat = "{0}?token={1}";

		// The current running WebSocket connection coroutine
		private Coroutine webSocketConnectCoroutine;

		// The last guest token used to connect the WebSocket
		private string lastGuestToken;

		/// <summary>
		/// If the user leaves the application and this option is enabled, close the WebSocket to avoid useless data transfers.
		/// When the user comes back, try to connect to the Geeo server again with the last used guest token.
		/// </summary>
		/// <param name="paused">If the application lost the focus.</param>
		private void OnApplicationPause(bool paused)
		{
			// Check if the "disconnect on application pause" option is enabled
			if (disconnectOnApplicationPause)
			{
				// User leaves the application (close the WebSocket connection but keep the guest token to connect again later)
				if (paused)
				{
					DebugLogs.LogWarning("[GeeoWS:OnApplicationPause] Application paused ›› Closing connection...");
					WebSocketClose();
				}
				// User resumes the application and there is a stored guest token (try to connect the WebSocket again)
				else if (lastGuestToken != null)
				{
					DebugLogs.LogWarning("[GeeoWS:OnApplicationPause] Application resumed ›› Reopening connection...");
					webSocketConnectCoroutine = Geeo.Instance.StartCoroutine(WebSocketConnect(lastGuestToken));
				}
			}
		}

		/// <summary>
		/// Close the WebSocket if application is killed.
		/// </summary>
		private void OnApplicationQuit()
		{
			DebugLogs.LogWarning("[GeeoWS:OnApplicationQuit] Application quit ›› Closing connection...");
			WebSocketClose();
		}

		/// <summary>
		/// Close the WebSocket if it is still opened.
		/// </summary>
		private void WebSocketClose()
		{
			// Stop the connect coroutine if still running
			if (webSocketConnectCoroutine != null)
			{
				Geeo.Instance.StopCoroutine(webSocketConnectCoroutine);
				webSocketConnectCoroutine = null;
			}

			// Stop the network check (ping)
			webSocket.NetworkCheckStop();

			// Clear the pending WebSocket messages list
			lock (pendingWebSocketMessages)
				pendingWebSocketMessages.Clear();

			// Close the WebSocket
			webSocket.Close();
		}

		/// <summary>
		/// Connect the WebSocket with a guest token previously provided by the Geeo server.
		/// </summary>
		/// <param name="guestToken">The guest token provided by the Geeo server.</param>
		private IEnumerator WebSocketConnect(string guestToken)
		{
			// Build the "websocket connect" request URL
			string requestUrl = string.Format(webSocketConnect_requestUrlFormat, wsUrl, guestToken);
			DebugLogs.LogVerbose("[GeeoWS:WebSocketConnect] Request URL: " + requestUrl);

			// Wait for the connection to be established
			yield return webSocket.Connect(requestUrl);

			// Start the network check (ping)
			webSocket.NetworkCheckStart();

			webSocketConnectCoroutine = null;
		}
		#endregion

		#region Messages Handling
		// The WebSocket received messages Actions queue to treat
		private List<Action> executingWebSocketMessages = new List<Action>();
		private List<Action> pendingWebSocketMessages = new List<Action>();


		#endregion
	}
}
