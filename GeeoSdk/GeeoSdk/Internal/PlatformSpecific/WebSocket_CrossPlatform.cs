#if !WEBGL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

using WebSocketSharp;

namespace GeeoSdk
{
	internal class WebSocket_CrossPlatform : WebSocket
	{
		private WebSocketSharp.WebSocket webSocketSharp;
		private bool error;

		#region Events Callbacks
		private Coroutine webSocketActionsCoroutine;

		// Use a pending delegates list to make sure the callback calls will be made from the main thread instead of
		// the ones created by the WebSocketSharp events to avoid "Unity classes not called from main thread" exceptions
		private List<Action> pendingWebSocketActions = new List<Action>();

		// Use a transitional delegates list to avoid "collection was modified; enumeration operation may not execute" exceptions
		private List<Action> executingWebSocketActions = new List<Action>();

		/// <summary>
		/// What to do when the current WebSocket is opened.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="eventData">Opened WebSocket event details.</param>
		private void OnWebSocketOpened(object sender, EventArgs eventData)
		{
			isConnected = true;

			if (OnOpenCallbacks != null)
				lock (pendingWebSocketActions)
					pendingWebSocketActions.Add(OnOpenCallbacks);
		}

		/// <summary>
		/// What to do when the current WebSocket gets an error.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="errorData">Error WebSocket event details.</param>
		private void OnErrorOccured(object sender, ErrorEventArgs errorData)
		{
			error = true;

			if (OnErrorCallbacks != null)
				lock (pendingWebSocketActions)
					pendingWebSocketActions.Add(delegate()
					{
						OnErrorCallbacks(errorData.Message);
					});
		}

		/// <summary>
		/// What to do when the current WebSocket receives a message.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="messageData">Message WebSocket event details.</param>
		private void OnMessageReceived(object sender, MessageEventArgs messageData)
		{
			if (OnMessageCallbacks != null)
				lock (pendingWebSocketActions)
					pendingWebSocketActions.Add(delegate()
					{
						OnMessageCallbacks(messageData.Data);
					});
		}

		/// <summary>
		/// What to do when the current WebSocket is closed.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="closeData">Closed WebSocket event details.</param>
		private void OnWebSocketClosed(object sender, CloseEventArgs closeData)
		{
			isConnected = false;

			if (OnCloseCallbacks != null)
				lock (pendingWebSocketActions)
					pendingWebSocketActions.Add(OnCloseCallbacks);
		}

		/// <summary>
		/// Each frame, execute then clear pending WebSocket delegates.
		/// </summary>
		private IEnumerator ExecuteWebSocketActions()
		{
			while (true)
			{
				if (pendingWebSocketActions.Count > 0)
					lock (pendingWebSocketActions)
					{
						executingWebSocketActions.AddRange(pendingWebSocketActions);
						pendingWebSocketActions.Clear();

						foreach (Action executingWebSocketMessage in executingWebSocketActions)
							executingWebSocketMessage();

						executingWebSocketActions.Clear();
					}

				yield return null;
			}
		}
		#endregion

		#region WebSocket Implement
		/// <summary>
		/// Create a new WebSocket and connect with the given URL.
		/// </summary>
		/// <param name="url">The endpoint URL to connect to.</param>
		public override IEnumerator Connect(string url)
		{
			if (webSocketActionsCoroutine == null)
				webSocketActionsCoroutine = Geeo.Instance.StartCoroutine(ExecuteWebSocketActions());
			
			error = false;
			webSocketSharp = new WebSocketSharp.WebSocket(url);
			webSocketSharp.OnOpen += OnWebSocketOpened;
			webSocketSharp.OnError += OnErrorOccured;
			webSocketSharp.OnMessage += OnMessageReceived;
			webSocketSharp.OnClose += OnWebSocketClosed;
			webSocketSharp.ConnectAsync();

			while (!isConnected && !error)
				yield return null;
		}

		/// <summary>
		/// Send a message through the current WebSocket.
		/// </summary>
		/// <param name="message">The message to send.</param>
		public override void Send(string message)
		{
			webSocketSharp.Send(message);
		}

		/// <summary>
		/// Close the current WebSocket connection.
		/// </summary>
		public override void Close()
		{
			if (isConnected)
				webSocketSharp.Close();
		}
		#endregion

		#region Network Check (Ping)
		private Thread networkCheckThread;

		/// <summary>
		/// Regularly check if the WebSocket is alive to detect potential network disconnections.
		/// </summary>
		private void NetworkCheck()
		{
			while (Thread.CurrentThread.IsAlive)
			{
				Thread.Sleep(networkCheckDelayMilliseconds);

				if (!webSocketSharp.IsAlive)
				{
					error = true;

					if (OnErrorCallbacks != null)
						lock (pendingWebSocketActions)
							pendingWebSocketActions.Add(delegate()
							{
								OnErrorCallbacks(networkCheckTimeoutMessage);
							});
				}
			}
		}

		/// <summary>
		/// Start the network check loop. (ping)
		/// </summary>
		public override void NetworkCheckStart()
		{
			if (networkCheckThread == null)
			{
				networkCheckThread = new Thread(new ThreadStart(NetworkCheck));
				networkCheckThread.Start();
			}
		}

		/// <summary>
		/// Stop the network check loop. (ping)
		/// </summary>
		public override void NetworkCheckStop()
		{
			if (networkCheckThread != null)
			{
				if (networkCheckThread.IsAlive)
					networkCheckThread.Abort();

				networkCheckThread = null;
			}
		}
		#endregion
	}
}
#endif
