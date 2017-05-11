using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using GeeoSdk;

namespace GeeoDemo
{
	/// <summary>
	/// A sample script to demonstrate how to use the Geeo SDK's features.
	/// Needs to be put as a component of an active object in the scene.
	/// </summary>
	public class DemoScript : MonoBehaviour
	{
		#region Geeo Handling
		/// <summary>
		/// At Start, connect with the Geeo server and register as a new agent with its viewport.
		/// </summary>
		private void Start()
		{
			// Check a Geeo instance exists in the scene
			if (Geeo.HasInstance == false)
			{
				LogAndDisplayError("No Geeo instance found ›› Please attach a ‘Geeo’ component on an active object of your scene!", "[DemoScript:Start]");
				return;
			}

			DisplayConnectionStatus(ConnectionStatus.Connecting);

			// Ask the Geeo server for a guest token (development only)
			Geeo.Instance.http.GetGuestToken("aID", "wpID", delegate(string guestToken)
				{
					Debug.Log("[DemoScript:GetGuestToken] Obtained guest token ›› " + guestToken);

					// Register callbacks for Geeo events
					Geeo.Instance.ws.OnConnected += OnConnected;
					Geeo.Instance.ws.OnDisconnected += OnDisconnected;

					// Connect to the Geeo server to start sending and receiving data
					Geeo.Instance.ws.Connect(guestToken);
				},
				delegate(string errorMessage)
				{
					LogAndDisplayError(errorMessage, "[DemoScript:GetGuestToken]");
					DisplayConnectionStatus(ConnectionStatus.Disconnected);
				});
		}

		/// <summary>
		/// Callback: the Geeo SDK just connected.
		/// </summary>
		private void OnConnected()
		{
			Debug.Log("[DemoScript:OnConnected] Geeo connected!");
			DisplayConnectionStatus(ConnectionStatus.Connected);
		}

		/// <summary>
		/// Callback: the Geeo SDK just disconnected.
		/// </summary>
		private void OnDisconnected()
		{
			Debug.Log("[DemoScript:OnDisconnected] Geeo disconnected!");
			DisplayConnectionStatus(ConnectionStatus.Disconnected);
		}
		#endregion

		#region Error Display
		// The error display UI elements and parameters
		[SerializeField] private GameObject errorPanel;
		[SerializeField] private Text errorText;
		[SerializeField] private float errorDisplayTime_Seconds = 5f;

		// The current running coroutine relative to the error display
		private Coroutine errorDisplayCoroutine;

		/// <summary>
		/// Log an error message and display it on the UI for a certain amount of time.
		/// </summary>
		/// <param name="errorMessage">The error description to log and display.</param>
		/// <param name="logPrefix">Any prefix to add only to the console log, and not on the UI.</param>
		private void LogAndDisplayError(string errorMessage, string logPrefix = null)
		{
			// Log the error message to the console
			Debug.LogError(string.IsNullOrEmpty(logPrefix) ? errorMessage : logPrefix + " " + errorMessage);

			// Display the error message on the UI for a certain amount of time
			if ((errorPanel != null) && (errorText != null))
			{
				// If a previous error display coroutine is still running, stop it
				if (errorDisplayCoroutine != null)
				{
					StopCoroutine(errorDisplayCoroutine);
					errorDisplayCoroutine = null;
				}

				// Set the error display text and activate the corresponding panel
				errorText.text = errorMessage;
				errorPanel.SetActive(true);

				// Start a coroutine to hide the error display panel in a certain amount of time
				StartCoroutine(HideError(errorDisplayTime_Seconds));
			}
		}

		/// <summary>
		/// Hide the error message from the UI.
		/// </summary>
		private IEnumerator HideError(float delay)
		{
			// Wait for the specified amount of time
			yield return new WaitForSeconds(delay);

			// Hide the error message display from the UI
			errorPanel.SetActive(false);
			errorDisplayCoroutine = null;
		}
		#endregion

		#region Connection Display
		// The connection display UI elements and parameters
		[SerializeField] private Image connectionStatusImage;
		[SerializeField] private Color connectionColor_Disconnected = new Color(1f, 0f, 0f, 0.78f);
		[SerializeField] private Color connectionColor_Connecting = new Color(1f, 1f, 0f, 0.78f);
		[SerializeField] private Color connectionColor_Connected = new Color(0f, 1f, 0f, 0.78f);

		// The available connection statutes
		private enum ConnectionStatus {Disconnected, Connecting, Connected}

		/// <summary>
		/// Display the connection status on the UI.
		/// </summary>
		/// <param name="connectionStatus">The current connection status.</param>
		private void DisplayConnectionStatus(ConnectionStatus connectionStatus)
		{
			// Change the connection status' UI image color depending on the current status
			switch (connectionStatus)
			{
				case ConnectionStatus.Disconnected:
				connectionStatusImage.color = connectionColor_Disconnected;
				break;

				case ConnectionStatus.Connecting:
				connectionStatusImage.color = connectionColor_Connecting;
				break;

				case ConnectionStatus.Connected:
				connectionStatusImage.color = connectionColor_Connected;
				break;
			}
		}
		#endregion
	}
}
