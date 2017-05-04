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
		// What to do when the current WebSocket is opened
		private void OnWebSocketOpened(object sender, EventArgs eventData)
		{
			isConnected = true;
			OnOpenCallbacks();
		}

		// What to do when the current WebSocket gets an error
		private void OnErrorOccured(object sender, ErrorEventArgs errorData)
		{
			error = true;
			OnErrorCallbacks(errorData.Message);
		}

		// What to do when the current WebSocket receives a message
		private void OnMessageReceived(object sender, MessageEventArgs messageData)
		{
			OnMessageCallbacks(messageData.Data);
		}

		// What to do when the current WebSocket is closed
		private void OnWebSocketClosed(object sender, CloseEventArgs closeData)
		{
			isConnected = false;
			OnCloseCallbacks();
		}
		#endregion

		#region WebSocket Implement
		// Create a new WebSocket and connect with the given URL
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

		// Send a message through the current WebSocket
		public override void Send(string message)
		{
			webSocketSharp.Send(message);
		}

		// Close the current WebSocket connection
		public override void Close()
		{
			if (isConnected)
				webSocketSharp.Close();
		}
		#endregion

		#region Network Check (Ping)
		// Regularly check if the WebSocket is alive to detect potential network disconnection while in a room
		private void NetworkCheck()
		{
			while (Thread.CurrentThread.IsAlive)
			{
				Thread.Sleep(networkCheckDelayMilliseconds);

				if (!webSocketSharp.IsAlive)
					OnErrorCallbacks(networkCheckTimeoutMessage);
			}
		}

		// Start the network check loop
		public override void NetworkCheckStart()
		{
			if (networkCheckThread == null)
			{
				networkCheckThread = new Thread(new ThreadStart(NetworkCheck));
				networkCheckThread.Start();
			}
		}

		// Stop the network check loop
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
