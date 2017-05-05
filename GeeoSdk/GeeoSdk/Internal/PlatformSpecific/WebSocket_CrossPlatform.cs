using System;
using System.Collections;
using System.Threading;

using WebSocketSharp;

namespace GeeoSdk
{
	internal class WebSocket_CrossPlatform : WebSocket
	{
		private WebSocketSharp.WebSocket webSocketSharp;
		private bool error;
		private Thread networkCheckThread;

		#region Events Callbacks
		/// <summary>
		/// What to do when the current WebSocket is opened.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="eventData">Opened WebSocket event details.</param>
		private void OnWebSocketOpened(object sender, EventArgs eventData)
		{
			isConnected = true;
			OnOpenCallbacks();
		}

		/// <summary>
		/// What to do when the current WebSocket gets an error.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="errorData">Error WebSocket event details.</param>
		private void OnErrorOccured(object sender, ErrorEventArgs errorData)
		{
			error = true;
			OnErrorCallbacks(errorData.Message);
		}

		/// <summary>
		/// What to do when the current WebSocket receives a message.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="messageData">Message WebSocket event details.</param>
		private void OnMessageReceived(object sender, MessageEventArgs messageData)
		{
			OnMessageCallbacks(messageData.Data);
		}

		/// <summary>
		/// What to do when the current WebSocket is closed.
		/// </summary>
		/// <param name="sender">Identifier of the instance which sent this event.</param>
		/// <param name="closeData">Closed WebSocket event details.</param>
		private void OnWebSocketClosed(object sender, CloseEventArgs closeData)
		{
			isConnected = false;
			OnCloseCallbacks();
		}
		#endregion

		#region WebSocket Implement
		/// <summary>
		/// Create a new WebSocket and connect with the given URL.
		/// </summary>
		/// <param name="url">The endpoint URL to connect to.</param>
		public override IEnumerator Connect(string url)
		{
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
		/// <summary>
		/// Regularly check if the WebSocket is alive to detect potential network disconnections.
		/// </summary>
		private void NetworkCheck()
		{
			while (Thread.CurrentThread.IsAlive)
			{
				Thread.Sleep(networkCheckDelayMilliseconds);

				if (!webSocketSharp.IsAlive)
					OnErrorCallbacks(networkCheckTimeoutMessage);
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
