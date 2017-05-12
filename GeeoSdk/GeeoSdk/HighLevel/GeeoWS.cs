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

		// The last token used to connect with the WebSocket
		private string lastWsToken;

		/// <summary>
		/// Connect the WebSocket with a token previously provided by the Geeo server.
		/// </summary>
		/// <param name="wsToken">The WebSocket token provided by the Geeo server.</param>
		private IEnumerator WebSocketConnect(string wsToken)
		{
			// Build the "websocket connect" request URL
			string requestUrl = string.Format(webSocketConnect_requestUrlFormat, wsUrl, wsToken);
			DebugLogs.LogVerbose("[GeeoWS:WebSocketConnect] Request URL: " + requestUrl);

			// Wait for the connection to be established
			yield return webSocket.Connect(requestUrl);

			// Start the network check (ping)
			webSocket.NetworkCheckStart();

			webSocketConnectCoroutine = null;
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

			// Close the WebSocket
			webSocket.Close();
		}

		/// <summary>
		/// If the user leaves the application and this option is enabled, close the WebSocket to avoid useless data transfers.
		/// When the user comes back, try to connect to the Geeo server again with the last used WebSocket token.
		/// </summary>
		/// <param name="paused">If the application lost the focus.</param>
		internal void OnApplicationPause(bool paused)
		{
			// Check if the "disconnect on application pause" option is enabled
			if (disconnectOnApplicationPause)
			{
				// User leaves the application (close the WebSocket connection but keep the WebSocket token to connect again later)
				if (paused && webSocket.isConnected)
				{
					DebugLogs.LogWarning("[GeeoWS:OnApplicationPause] Application paused ›› Closing connection...");
					WebSocketClose();
				}
				// User resumes the application and there is a stored WebSocket token (try to connect the WebSocket again)
				else if (!paused && !string.IsNullOrEmpty(lastWsToken))
				{
					DebugLogs.LogWarning("[GeeoWS:OnApplicationPause] Application resumed ›› Reopening connection...");
					webSocketConnectCoroutine = Geeo.Instance.StartCoroutine(WebSocketConnect(lastWsToken));
				}
			}
		}

		/// <summary>
		/// Close the WebSocket when application is killed.
		/// </summary>
		internal void OnApplicationQuit()
		{
			DebugLogs.LogWarning("[GeeoWS:OnApplicationQuit] Application quit ›› Closing connection...");
			WebSocketClose();
		}
		#endregion

		#region WebSocket Internal Events
		/// <summary>
		/// Listener to react when a "WebSocket opened" event occurs.
		/// </summary>
		private void OnWebSocketOpen()
		{
			DebugLogs.LogVerbose("[GeeoWS:OnWebSocketOpen] WebSocket opened");
			OnConnected();
		}

		/// <summary>
		/// Listener to react when a "WebSocket closed" event occurs.
		/// </summary>
		private void OnWebSocketClose()
		{
			DebugLogs.LogWarning("[GeeoWS:OnWebSocketClose] WebSocket closed");
			OnDisconnected();
		}

		/// <summary>
		/// Listener to react when a "WebSocket error" event occurs.
		/// </summary>
		/// <param name="error">The error's message.</param>
		private void OnWebSocketError(string error)
		{
			DebugLogs.LogError("[GeeoWS:OnWebSocketError] WebSocket error ›› " + error);
			OnError(error);
		}

		/// <summary>
		/// Listener to react when a "WebSocket message" event occurs.
		/// </summary>
		/// <param name="message">The received message.</param>
		private void OnWebSocketMessage(string message)
		{
			DebugLogs.LogVerbose("[GeeoWS:OnWebSocketMessage] WebSocket message ›› " + message);

			// TODO: Treat messages >> ... events callbacks
		}
		#endregion

		#region WebSocket Public Events
		// Callback: the WebSocket just connected
		public event Action OnConnected;

		// Callback: the WebSocket just disconnected
		public event Action OnDisconnected;

		// Callback: the WebSocket just encountered an error
		public event Action<string> OnError;

		// TODO: Public Geeo relative events: (OnPoiEntered, OnPoiLeft, etc...)
		#endregion

		#region Requests Handling
		/// <summary>
		/// Use a WebSocket token previously provided by the Geeo server to open a WebSocket connection.
		/// If development routes are allowed, you may use the GeeoHTTP.GetGuestToken() method to get a token.
		/// Once the connection is opened, the OnConnected event will be triggered (so you should register a callback to it).
		/// </summary>
		/// <param name="wsToken">The WebSocket token previously provided by the Geeo server.</param>
		public void Connect(string wsToken)
		{
			DebugLogs.LogVerbose("[GeeoWS:Connect] Connecting...");

			// Keep the token used to connect the WebSocket
			lastWsToken = wsToken;

			// Register listeners for future WebSocket events
			webSocket.OnOpen += OnWebSocketOpen;
			webSocket.OnClose += OnWebSocketClose;
			webSocket.OnError += OnWebSocketError;
			webSocket.OnMessage += OnWebSocketMessage;

			// Create a WebSocket and connect to the Geeo server
			webSocketConnectCoroutine = Geeo.Instance.StartCoroutine(WebSocketConnect(wsToken));
		}

		/// <summary>
		/// Remove the last used WebSocket token and registered listeners, then close the current WebSocket connection.
		/// Once the connection is closed, the OnDisconnected event will be triggered (so you should register a callback to it).
		/// </summary>
		public void Disconnect()
		{
			DebugLogs.LogVerbose("[GeeoWS:Disconnect] Disconnecting...");

			// Unvalidate the last used WebSocket token
			lastWsToken = null;

			// Unregister listeners for future WebSocket events
			webSocket.OnOpen -= OnWebSocketOpen;
			webSocket.OnError -= OnWebSocketError;
			webSocket.OnMessage -= OnWebSocketMessage;

			// Disconnect from the Geeo server
			WebSocketClose();

			// We unregister the OnClose listener only after the close call to get this event trigger
			webSocket.OnClose -= OnWebSocketClose;
		}
		#endregion
	}
}
