using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace GeeoSdk
{
	internal class WebSocket_WebGL : WebSocket
	{
		#region Static Library Entry Points
		[DllImport("__Internal")] private static extern int SocketCreate(string url);
		[DllImport("__Internal")] private static extern int SocketState(int socketInstance);
		[DllImport("__Internal")] private static extern int SocketError(int socketInstance, byte[] ptr, int length);
		[DllImport("__Internal")] private static extern void SocketSend(int socketInstance, byte[] ptr, int length);
		[DllImport("__Internal")] private static extern void SocketSendString(int socketInstance, byte[] ptr, int length);
		[DllImport("__Internal")] private static extern int SocketRecvLength(int socketInstance);
		[DllImport("__Internal")] private static extern void SocketRecv(int socketInstance, byte[] ptr, int length);
		[DllImport("__Internal")] private static extern void SocketRecvString(int socketInstance, byte[] ptr, int length);
		[DllImport("__Internal")] private static extern void SocketClose(int socketInstance);
		#endregion

		private const int bufferSize = 1024;

		private int nativeReference = 0;
		private string currentError;
		private string currentMessage;
		private Coroutine runningCoroutine;
		private Coroutine networkCheckCoroutine;

		#region Events Callbacks
		/// <summary>
		/// Check if any error has occurred or any message has been received.
		/// </summary>
		private IEnumerator CheckForErrorsAndMessages()
		{
			while (isConnected)
			{
				if ((currentError = CheckForErrors()) != null)
					OnErrorCallbacks(currentError);

				if ((currentMessage = CheckForMessages()) != null)
					OnMessageCallbacks(currentMessage);

				yield return null;
			}
		}

		/// <summary>
		/// Check if any error has occurred.
		/// </summary>
		private string CheckForErrors()
		{
			byte[] buffer = new byte[bufferSize];
			int result = SocketError(nativeReference, buffer, bufferSize);

			if (result == 0)
				return null;

			return Encoding.UTF8.GetString(buffer);				
		}

		/// <summary>
		/// Check if any message has been received.
		/// </summary>
		private string CheckForMessages()
		{
			int length = SocketRecvLength(nativeReference);

			if (length == 0)
				return null;

			byte[] buffer = new byte[length];
			SocketRecvString(nativeReference, buffer, length);

			if (buffer == null)
				return null;

			return Encoding.UTF8.GetString(buffer);
		}

		/// <summary>
		/// Start checking for errors and messages.
		/// </summary>
		private void StartRunningCoroutine()
		{
			StopRunningCoroutine();
			runningCoroutine = Geeo.Instance.StartCoroutine(CheckForErrorsAndMessages());
		}

		/// <summary>
		/// Stop checking for errors and messages.
		/// </summary>
		private void StopRunningCoroutine()
		{
			if (runningCoroutine != null)
			{
				Geeo.Instance.StopCoroutine(runningCoroutine);
				runningCoroutine = null;
			}
		}
		#endregion

		#region WebSocket Implement & Events Callbacks
		/// <summary>
		/// Create a new WebSocket and connect with the given URL.
		/// </summary>
		/// <param name="url">The endpoint URL to connect to.</param>
		public override IEnumerator Connect(string url)
		{
			nativeReference = SocketCreate(url);

			while (SocketState(nativeReference) == 0)
				yield return null;

			isConnected = true;
			OnOpenCallbacks();
			StartRunningCoroutine();
		}

		/// <summary>
		/// Send a message through the current WebSocket.
		/// </summary>
		/// <param name="message">The message to send.</param>
		public override void Send(string message)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(message);
			SocketSendString(nativeReference, buffer, buffer.Length);
		}

		/// <summary>
		/// Close the current WebSocket connection.
		/// </summary>
		public override void Close()
		{
			if (isConnected)
			{
				StopRunningCoroutine();
				SocketClose(nativeReference);
				isConnected = false;
				OnCloseCallbacks();
			}
		}
		#endregion

		#region Network Check (Ping)
		/// <summary>
		/// Regularly check if the WebSocket is alive to detect potential network disconnections.
		/// </summary>
		private IEnumerator NetworkCheck()
		{
			while (true)
			{
				yield return new WaitForSecondsRealtime(networkCheckDelaySeconds);

				if (SocketState(nativeReference) != 1)
					OnErrorCallbacks(networkCheckTimeoutMessage);
			}
		}

		/// <summary>
		/// Start the network check loop. (ping)
		/// </summary>
		public override void NetworkCheckStart()
		{
			if (networkCheckCoroutine == null)
				networkCheckCoroutine = Geeo.Instance.StartCoroutine(NetworkCheck());
		}

		/// <summary>
		/// Stop the network check loop. (ping)
		/// </summary>
		public override void NetworkCheckStop()
		{
			if (networkCheckCoroutine != null)
			{
				Geeo.Instance.StopCoroutine(networkCheckCoroutine);
				networkCheckCoroutine = null;
			}
		}
		#endregion
	}
}
