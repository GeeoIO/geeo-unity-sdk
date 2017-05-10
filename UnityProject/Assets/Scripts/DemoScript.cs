using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using GeeoSdk;

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

		// Ask the Geeo server for a guest token
		Geeo.Instance.http.GetGuestToken("aID", "wpID", delegate(string guestToken)
			{
				Debug.Log("[DemoScript:GetGuestToken] Obtained guest token ›› " + guestToken);

				Geeo.Instance.ws.Connect(guestToken);
			},
			delegate(string errorMessage)
			{
				LogAndDisplayError(errorMessage, "[DemoScript:GetGuestToken]");
			});
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
}
