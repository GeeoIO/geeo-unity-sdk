﻿using System;
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
		// The format to get an ISO 8601 DateTime string
		private const string dateTimeFormat_Iso8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";

		/// <summary>
		/// At Start, connect with the Geeo server and register as a new agent with its associated viewport.
		/// </summary>
		private void Start()
		{
			// Check a Geeo instance exists in the scene
			if (Geeo.HasInstance == false)
			{
				LogAndDisplayError("No Geeo instance ›› Please attach a ‘Geeo’ component on an active object of your scene!", "[DemoScript:Start]");
				return;
			}

			Debug.Log("[DemoScript:Start] Connecting to the Geeo server...");
			DisplayStatus(Status.Geeo, StatusState.Initializing);

			// Generate pseudo-random agent and viewport identifiers
			string currentDateTime = DateTime.UtcNow.ToString(dateTimeFormat_Iso8601);
			string agentId = "agent" + currentDateTime;
			string viewportId = "view" + currentDateTime;

			// Ask the Geeo server for a guest token (development only)
			Geeo.Instance.http.GetGuestToken(agentId, viewportId, delegate(string guestToken)
				{
					Debug.Log("[DemoScript:GetGuestToken] Obtained guest token ›› " + guestToken);

					// Register callbacks for Geeo events
					Geeo.Instance.ws.OnConnected += OnGeeoConnected;
					Geeo.Instance.ws.OnDisconnected += OnGeeoDisconnected;
					Geeo.Instance.ws.OnError += OnGeeoError;

					// Connect to the Geeo server to start sending and receiving data
					Geeo.Instance.ws.Connect(guestToken);
				},
				delegate(string errorMessage)
				{
					LogAndDisplayError("Geeo error: " + errorMessage, "[DemoScript:GetGuestToken]");
					DisplayStatus(Status.Geeo, StatusState.Stopped);
				});
		}

		/// <summary>
		/// Callback: the Geeo SDK just connected.
		/// </summary>
		private void OnGeeoConnected()
		{
			Debug.Log("[DemoScript:OnGeeoConnected] Geeo connected ›› Starting user location: " + lastUserLocation.ToString());
			DisplayStatus(Status.Geeo, StatusState.Started);

			// Start the location service to get the user location
			runningUserLocationUpdateCoroutine = StartCoroutine(StartUserLocationUpdate());
		}

		/// <summary>
		/// Callback: the Geeo SDK just disconnected.
		/// </summary>
		private void OnGeeoDisconnected()
		{
			Debug.LogWarning("[DemoScript:OnGeeoDisconnected] Geeo disconnected");
			DisplayStatus(Status.Geeo, StatusState.Stopped);

			// Stop the location service (no need to get the user location anymore)
			StopUserLocationUpdate();
		}

		/// <summary>
		/// Callback: the Geeo SDK just encountered an error.
		/// </summary>
		/// <param name="error">The error message.</param>
		private void OnGeeoError(string error)
		{
			LogAndDisplayError("Geeo error: " + error, "[DemoScript:OnGeeoError]");
		}
		#endregion

		#region User Location
		/// <summary>
		/// Represents a user location with its latitude and longitude coordinates.
		/// </summary>
		private class UserLocation
		{
			public float latitude;
			public float longitude;

			/// <summary>
			/// Class constructor.
			/// </summary>
			/// <param name="_latitude">User's location latitude.</param>
			/// <param name="_longitude">User's location longitude.</param>
			public UserLocation(float _latitude, float _longitude)
			{
				latitude = _latitude;
				longitude= _longitude;
			}

			/// <summary>
			/// Converts the value of this instance to its equivalent string representation.
			/// </summary>
			public override string ToString()
			{
				return string.Format ("{{{0}, {1}}}", latitude, longitude);
			}
		}

		// The last user location obtained from the location service
		// If no user location can be obtained from the location service, let's say you're in Tenerife by default
		private UserLocation lastUserLocation = new UserLocation(28.04798f, -16.71737f);

		// The current running user location update coroutine
		private Coroutine runningUserLocationUpdateCoroutine;

		// Time to wait (in seconds) between each location service initialization check
		private const float locationServiceInitChecksDelay = 0.05f;

		// Time to wait (in seconds) between each update of the user location from the location service
		private const float locationServiceUserLocationUpdatesDelay = 1f;

		/// <summary>
		/// Start continuously querying user location.
		/// </summary>
		private IEnumerator StartUserLocationUpdate()
		{
			Debug.Log("[DemoScript:StartUserLocationUpdate] User location service starting...");
			DisplayStatus(Status.Location, StatusState.Initializing);

			// First, check if user has location service enabled
			if (!Input.location.isEnabledByUser)
			{
				LogAndDisplayError("Location service not enabled by user", "[DemoScript:StartUserLocationUpdate]");
				DisplayStatus(Status.Location, StatusState.Stopped);
				runningUserLocationUpdateCoroutine = null;
				yield break;
			}

			// Start location service before querying location
			Input.location.Start();

			// Wait until service initializes
			while (Input.location.status == LocationServiceStatus.Initializing)
				yield return new WaitForSeconds(locationServiceInitChecksDelay);

			// Connection has failed
			if (Input.location.status == LocationServiceStatus.Failed)
			{
				LogAndDisplayError("Unable to start location service", "[DemoScript:StartUserLocationUpdate]");
				DisplayStatus(Status.Location, StatusState.Stopped);
				runningUserLocationUpdateCoroutine = null;
				yield break;
			}
			// Access granted and location value could be retrieved
			else
			{
				Debug.Log("[DemoScript:StartUserLocationUpdate] User location service started");
				DisplayStatus(Status.Location, StatusState.Started);

				// Continuously query user location with some delay between each query
				while (true)
				{
					yield return new WaitForSeconds(locationServiceUserLocationUpdatesDelay);

					lastUserLocation.latitude = Input.location.lastData.latitude;
					lastUserLocation.longitude = Input.location.lastData.longitude;
					Debug.Log("[DemoScript:StartUserLocationUpdate] Last user location: " + lastUserLocation.ToString());
				}
			}
		}

		/// <summary>
		/// Stop querying user location.
		/// </summary>
		private void StopUserLocationUpdate()
		{
			// Stop the current running user location update coroutine
			if (runningUserLocationUpdateCoroutine != null)
			{
				StopCoroutine(runningUserLocationUpdateCoroutine);
				runningUserLocationUpdateCoroutine = null;
			}

			// Stop location service
				Input.location.Stop();

			Debug.LogWarning("[DemoScript:StopUserLocationUpdate] User location service stopped");
			DisplayStatus(Status.Location, StatusState.Stopped);
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
		/// <param name="delay">Time to wait (in seconds) before hiding the error message.</param>
		private IEnumerator HideError(float delay)
		{
			// Wait for the specified amount of time
			yield return new WaitForSeconds(delay);

			// Hide the error message display from the UI
			errorPanel.SetActive(false);
			errorDisplayCoroutine = null;
		}
		#endregion

		#region Status Display
		// The status display UI elements and parameters
		[SerializeField] private Image geeoStatusImage;
		[SerializeField] private Image locationStatusImage;
		[SerializeField] private Color stoppedStatusState = new Color(1f, 0f, 0f, 0.78f);
		[SerializeField] private Color initializingStatusState = new Color(1f, 1f, 0f, 0.78f);
		[SerializeField] private Color startedStatusState = new Color(0f, 1f, 0f, 0.78f);

		// The available statutes
		private enum Status {Geeo, Location}
		private enum StatusState {Stopped, Initializing, Started}

		/// <summary>
		/// Display a status on the UI.
		/// </summary>
		/// <param name="status">The status to update.</param>
		/// <param name="statusState">The status state to display.</param>
		private void DisplayStatus(Status status, StatusState statusState)
		{
			// Get the status' UI image
			Image statusImage;

			switch (status)
			{
				case Status.Geeo:
				statusImage = geeoStatusImage;
				break;

				case Status.Location:
				statusImage = locationStatusImage;
				break;

				default: return;
			}

			// Change the status' UI image color depending on the current status state
			switch (statusState)
			{
				case StatusState.Stopped:
				statusImage.color = stoppedStatusState;
				break;

				case StatusState.Initializing:
				statusImage.color = initializingStatusState;
				break;

				case StatusState.Started:
				statusImage.color = startedStatusState;
				break;

				default: return;
			}
		}
		#endregion
	}
}
