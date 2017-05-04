using System;
using System.Collections;

namespace GeeoSdk
{
	internal abstract class WebSocket
	{
		#region Events Callbacks
		// Callback: a new WebSocket just connected
		protected Action OnOpenCallbacks;
		public event Action OnOpen
		{
			add {OnOpenCallbacks += value;}
			remove {OnOpenCallbacks -= value;}
		}

		// Callback: the current WebSocket just received a message
		protected Action<string> OnMessageCallbacks;
		public event Action<string> OnMessage
		{
			add {OnMessageCallbacks += value;}
			remove {OnMessageCallbacks -= value;}
		}

		// Callback: the current WebSocket just getted an error
		protected Action<string> OnErrorCallbacks;
		public event Action<string> OnError
		{
			add {OnErrorCallbacks += value;}
			remove {OnErrorCallbacks -= value;}
		}

		// Callback: the current WebSocket just closed
		protected Action OnCloseCallbacks;
		public event Action OnClose
		{
			add {OnCloseCallbacks += value;}
			remove {OnCloseCallbacks -= value;}
		}
		#endregion

		#region WebSocket Implement
		// If the current WebSocket is currently opened
		public bool isConnected {get; protected set;}

		// Create a new WebSocket and connect with the given URL
		public abstract IEnumerator Connect(string url);

		// Send a message through the current WebSocket
		public abstract void Send(string message);

		// Close the current WebSocket connection
		public abstract void Close();
		#endregion

		#region Network Check (Ping)
		// Delay after a network check finished before starting the next one
		protected const float networkCheckDelaySeconds = 3f;
		protected const int networkCheckDelayMilliseconds = (int)(networkCheckDelaySeconds * 1000f);
		protected const string networkCheckTimeoutMessage = "Ping timeout reached";

		// Start the network check loop
		public abstract void NetworkCheckStart();

		// Stop the network check loop
		public abstract void NetworkCheckStop();
		#endregion
	}
}
