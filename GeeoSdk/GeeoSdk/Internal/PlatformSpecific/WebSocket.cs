using System;
using System.Collections;

namespace GeeoSdk
{
	internal abstract class WebSocket
	{
		#region Events Callbacks
		protected Action OnOpenCallbacks;
		/// <summary>
		/// Callback: a new WebSocket just connected.
		/// </summary>
		public event Action OnOpen
		{
			add {OnOpenCallbacks += value;}
			remove {OnOpenCallbacks -= value;}
		}

		protected Action<string> OnMessageCallbacks;
		/// <summary>
		/// Callback: the current WebSocket just received a message.
		/// </summary>
		public event Action<string> OnMessage
		{
			add {OnMessageCallbacks += value;}
			remove {OnMessageCallbacks -= value;}
		}

		protected Action<string> OnErrorCallbacks;
		/// <summary>
		/// Callback: the current WebSocket just got an error.
		/// </summary>
		public event Action<string> OnError
		{
			add {OnErrorCallbacks += value;}
			remove {OnErrorCallbacks -= value;}
		}

		protected Action OnCloseCallbacks;
		/// <summary>
		/// Callback: the current WebSocket just closed.
		/// </summary>
		public event Action OnClose
		{
			add {OnCloseCallbacks += value;}
			remove {OnCloseCallbacks -= value;}
		}
		#endregion

		#region WebSocket Implement
		/// <summary>
		/// If the current WebSocket is currently opened.
		/// </summary>
		public bool isConnected {get; protected set;}

		/// <summary>
		/// Create a new WebSocket and connect with the given URL.
		/// </summary>
		/// <param name="url">The endpoint URL to connect to.</param>
		public abstract IEnumerator Connect(string url);

		/// <summary>
		/// Send a message through the current WebSocket.
		/// </summary>
		/// <param name="message">The message to send.</param>
		public abstract void Send(string message);

		/// <summary>
		/// Close the current WebSocket connection.
		/// </summary>
		public abstract void Close();
		#endregion

		#region Network Check (Ping)
		// Delay after a network check finished before starting the next one
		public float networkCheckDelaySeconds = 30f;
		public int networkCheckDelayMilliseconds = 30000;
		protected const string networkCheckTimeoutMessage = "Ping timeout reached";

		/// <summary>
		/// Start the network check loop. (ping)
		/// </summary>
		public abstract void NetworkCheckStart();

		/// <summary>
		/// Stop the network check loop. (ping)
		/// </summary>
		public abstract void NetworkCheckStop();
		#endregion
	}
}
