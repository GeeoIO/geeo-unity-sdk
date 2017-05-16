using System;
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
		#region Geeo SDK Initialization
		// The format to get an ISO 8601 DateTime string
		private const string dateTimeFormat_Iso8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";

		// Currently connected Geeo agent and viewport identifiers
		private string currentAgentId;
		private string currentViewportId;

		/// <summary>
		/// At Start, set default user location and view, then initialize the Geeo SDK.
		/// </summary>
		private void Start()
		{
			// Set the default user location and view in allowed GPS bounds
			lastUserLocation = new UserLocation(Math.Min(Math.Max(defaultUserLocationLatitude, latitudeMin), latitudeMax),
				Math.Min(Math.Max(defaultUserLocationLongitude, longitudeMin), longitudeMax),
				userLocationDisplayPointPrefab, displayMap.transform);

			lastUserView = new UserView(Math.Min(Math.Max(defaultUserLocationLatitude - userViewLatitudeExtent, latitudeMin), latitudeMax),
				Math.Min(Math.Max(defaultUserLocationLatitude + userViewLatitudeExtent, latitudeMin), latitudeMax),
				Math.Min(Math.Max(defaultUserLocationLongitude - userViewLongitudeExtent, longitudeMin), longitudeMax),
				Math.Min(Math.Max(defaultUserLocationLongitude + userViewLongitudeExtent, longitudeMin), longitudeMax),
				lastUserLocation.displayPoint);

			// Initialize the Geeo SDK
			Geeo_InitializeSdk();
		}

		/// <summary>
		/// Connect with the Geeo server and register as a new agent with its associated viewport.
		/// </summary>
		private void Geeo_InitializeSdk()
		{
			// Check a Geeo instance exists in the scene
			if (Geeo.HasInstance == false)
			{
				LogAndDisplayError("No Geeo instance ›› Please attach a ‘Geeo’ component on an active object of your scene!", "[DemoScript:Geeo_InitializeSdk]");
				return;
			}

			Debug.Log("[DemoScript:Geeo_InitializeSdk] Connecting to the Geeo server...");
			DisplayStatus(Status.Geeo, StatusState.Initializing);

			// Generate pseudo-random agent and viewport identifiers
			string currentDateTime = DateTime.UtcNow.ToString(dateTimeFormat_Iso8601);
			currentAgentId = "agent" + currentDateTime;
			currentViewportId = "view" + currentDateTime;

			// Ask the Geeo server for a guest token (development only)
			Geeo.Instance.http.GetGuestToken(currentAgentId, currentViewportId, delegate(string guestToken)
				{
					Debug.Log("[DemoScript:Geeo_InitializeSdk] Obtained guest token ›› " + guestToken);

					// Register callbacks for Geeo events
					Geeo.Instance.ws.OnConnected += Geeo_OnConnected;
					Geeo.Instance.ws.OnDisconnected += Geeo_OnDisconnected;
					Geeo.Instance.ws.OnError += Geeo_OnError;

					// Connect to the Geeo server to start sending and receiving data
					Geeo.Instance.ws.Connect(guestToken);
				},
				delegate(string errorMessage)
				{
					LogAndDisplayError("Geeo error: " + errorMessage, "[DemoScript:Geeo_InitializeSdk]");
					DisplayStatus(Status.Geeo, StatusState.Stopped);
				});
		}
		#endregion

		#region Geeo SDK Events
		/// <summary>
		/// Callback: the Geeo SDK just connected.
		/// </summary>
		private void Geeo_OnConnected()
		{
			Debug.Log("[DemoScript:Geeo_OnConnected] Geeo connected ›› Starting user location: " + lastUserLocation + ", Starting user view: " + lastUserView);
			DisplayStatus(Status.Geeo, StatusState.Started);

			// Show the displayed used location
			DisplayUserLocation(true);

			// To get started, send a move with the default user location and view
			Geeo_MoveConnectedAgent(lastUserLocation);
			Geeo_MoveConnectedViewport(lastUserView);

			// Start the location service to get the user location (or use the simulated user location if enabled)
			runningUserLocationUpdateCoroutine = StartCoroutine(useSimulatedUserLocation ? StartSimulatedUserLocationUpdate() : StartUserLocationUpdate());
		}

		/// <summary>
		/// Callback: the Geeo SDK just disconnected.
		/// </summary>
		private void Geeo_OnDisconnected()
		{
			Debug.LogWarning("[DemoScript:Geeo_OnDisconnected] Geeo disconnected");
			DisplayStatus(Status.Geeo, StatusState.Stopped);

			// Hide the displayed used location
			DisplayUserLocation(false);

			// Stop the location service (no need to get the user location anymore)
			StopUserLocationUpdate();
		}

		/// <summary>
		/// Callback: the Geeo SDK just encountered an error.
		/// </summary>
		/// <param name="error">The error message.</param>
		private void Geeo_OnError(string error)
		{
			LogAndDisplayError("Geeo error: " + error, "[DemoScript:Geeo_OnError]");
		}
		#endregion

		#region Geeo SDK Requests
		/// <summary>
		/// Update the Geeo's connected agent location.
		/// </summary>
		/// <param name="currentAgentLocation">The new last known user agent location.</param>
		private void Geeo_MoveConnectedAgent(UserLocation currentAgentLocation)
		{
			Geeo.Instance.ws.MoveConnectedAgent(currentAgentLocation.latitude, currentAgentLocation.longitude);
		}

		/// <summary>
		/// Update the Geeo's connected viewport location.
		/// </summary>
		/// <param name="currentViewportLocation">The new last known user view location.</param>
		private void Geeo_MoveConnectedViewport(UserView currentViewportLocation)
		{
			Geeo.Instance.ws.MoveConnectedViewport(currentViewportLocation.latitude1, currentViewportLocation.latitude2, currentViewportLocation.longitude1, currentViewportLocation.longitude2);
		}
		#endregion

		#region User Location And View
		/// <summary>
		/// Represents a user location with its latitude and longitude coordinates.
		/// </summary>
		private class UserLocation
		{
			// User location's coordinates
			public double latitude;
			public double longitude;

			// User location's display point object
			public GameObject displayPoint;

			/// <summary>
			/// Class constructor.
			/// </summary>
			/// <param name="_latitude">User's location latitude.</param>
			/// <param name="_longitude">User's location longitude.</param>
			/// <param name="_displayPointPrefab">Prefab of user's display point.</param>
			/// <param name="_displayMap">User's display point map parent.</param>
			public UserLocation(double _latitude, double _longitude, GameObject _displayPointPrefab, Transform _displayMap)
			{
				latitude = _latitude;
				longitude = _longitude;
				displayPoint = Instantiate(_displayPointPrefab, _displayMap, false);
				displayPoint.SetActive(false);
			}

			/// <summary>
			/// Converts the value of this instance to its equivalent string representation.
			/// {latitude, longitude}
			/// </summary>
			public override string ToString()
			{
				return string.Format ("{{ La: {0}, Lo: {1} }}", latitude, longitude);
			}
		}

		/// <summary>
		/// Represents a user view with its latitudes and longitudes bounds coordinates.
		/// </summary>
		private class UserView
		{
			// User view's coordinates
			public double latitude1;
			public double latitude2;
			public double longitude1;
			public double longitude2;

			// User view's display LineRender instance
			public LineRenderer displayLines;

			/// <summary>
			/// Class constructor.
			/// </summary>
			/// <param name="_latitude1">First user's view latitude bound.</param>
			/// <param name="_latitude2">Second user's view latitude bound.</param>
			/// <param name="_longitude1">First user's view longitude bound.</param>
			/// <param name="_longitude2">Second user's view longitude bound.</param>
			/// <param name="_displayPoint">User's display point reference.</param>
			public UserView(double _latitude1, double _latitude2, double _longitude1, double _longitude2, GameObject _displayPoint)
			{
				latitude1 = _latitude1;
				latitude2 = _latitude2;
				longitude1 = _longitude1;
				longitude2 = _longitude2;
				displayLines = _displayPoint.GetComponent<LineRenderer>();
			}

			/// <summary>
			/// Converts the value of this instance to its equivalent string representation.
			/// {latitude1, latitude2, longitude1, longitude2}
			/// </summary>
			public override string ToString()
			{
				return string.Format ("{{ La1: {0}, La2: {1}, Lo1: {2}, Lo2: {3} }}", latitude1, latitude2, longitude1, longitude2);
			}
		}

		// Time to wait (in seconds) between each location service initialization check
		private const float locationServiceInitChecksDelay = 1f;

		// GPS coordinates constants
		private const double latitudeMin = -85.05112878d;
		private const double latitudeMax = 85.05112878d;
		private const double longitudeMin = -180d;
		private const double longitudeMax = 180d;

		// Time to wait (in seconds) between each update of the user location from the location service
		[SerializeField] private float userLocationUpdatesDelay = 1f;

		// If continuous user locations should be simulated instead of using the location service
		[SerializeField] private bool useSimulatedUserLocation = false;

		// The range of maximum allowed simulated moves on latitude and longitude per update
		[SerializeField] private float simulatedUserLocationMoveRange = 1f;

		// As long as no user location can be obtained from the location service, let's say you're in Tenerife by default
		[SerializeField] private double defaultUserLocationLatitude = 28.0479823d;
		[SerializeField] private double defaultUserLocationLongitude = -16.7173771d;

		// How much latitude/longitude to add/subtract to user's location to get its view bounds
		[SerializeField] private double userViewLatitudeExtent = 10f;
		[SerializeField] private double userViewLongitudeExtent = 20f;

		// The last user location obtained from the location service
		private UserLocation lastUserLocation;

		// The last user view extended from the last user location
		private UserView lastUserView;

		// The current running user location update coroutine
		private Coroutine runningUserLocationUpdateCoroutine;

		/// <summary>
		/// Start updating the current user location with simulated moves.
		/// </summary>
		private IEnumerator StartSimulatedUserLocationUpdate()
		{
			Debug.Log("[DemoScript:StartSimulatedUserLocationUpdate] Simulated user location moves started");
			DisplayStatus(Status.Location, StatusState.Started);

			// Continuously simulate user location moves with some delay between each move
			while (true)
			{
				// Wait for a certain delay before the next query
				yield return new WaitForSeconds(userLocationUpdatesDelay);

				// Update the last user location and view data in allowed GPS bounds
				lastUserLocation.latitude = Math.Min(Math.Max(lastUserLocation.latitude + (double)UnityEngine.Random.Range(-simulatedUserLocationMoveRange, simulatedUserLocationMoveRange), latitudeMin), latitudeMax);
				lastUserLocation.longitude = Math.Min(Math.Max(lastUserLocation.longitude + (double)UnityEngine.Random.Range(-simulatedUserLocationMoveRange, simulatedUserLocationMoveRange), longitudeMin), longitudeMax);
				lastUserView.latitude1 = Math.Min(Math.Max(lastUserLocation.latitude - userViewLatitudeExtent, latitudeMin), latitudeMax);
				lastUserView.latitude2 = Math.Min(Math.Max(lastUserLocation.latitude + userViewLatitudeExtent, latitudeMin), latitudeMax);
				lastUserView.longitude1 = Math.Min(Math.Max(lastUserLocation.longitude - userViewLongitudeExtent, longitudeMin), longitudeMax);
				lastUserView.longitude2 = Math.Min(Math.Max(lastUserLocation.longitude + userViewLongitudeExtent, longitudeMin), longitudeMax);
				Debug.Log("[DemoScript:StartSimulatedUserLocationUpdate] Last user location: " + lastUserLocation + ", Last user view: " + lastUserView);

				// Display the new user's location and view
				DisplayUserLocation(true);

				// Send a move to update the Geeo's user location and view
				Geeo_MoveConnectedAgent(lastUserLocation);
				Geeo_MoveConnectedViewport(lastUserView);
			}
		}

		/// <summary>
		/// Start continuously querying the current user location to the device location service.
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

			// Start the location service before querying location
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
					// Wait for a certain delay before the next query
					yield return new WaitForSeconds(userLocationUpdatesDelay);

					// Update the last user location and view data in allowed GPS bounds
					lastUserLocation.latitude = (double)Input.location.lastData.latitude;
					lastUserLocation.longitude = (double)Input.location.lastData.longitude;
					lastUserView.latitude1 = Math.Min(Math.Max(lastUserLocation.latitude - userViewLatitudeExtent, latitudeMin), latitudeMax);
					lastUserView.latitude2 = Math.Min(Math.Max(lastUserLocation.latitude + userViewLatitudeExtent, latitudeMin), latitudeMax);
					lastUserView.longitude1 = Math.Min(Math.Max(lastUserLocation.longitude - userViewLongitudeExtent, longitudeMin), longitudeMax);
					lastUserView.longitude2 = Math.Min(Math.Max(lastUserLocation.longitude + userViewLongitudeExtent, longitudeMin), longitudeMax);
					Debug.Log("[DemoScript:StartUserLocationUpdate] Last user location: " + lastUserLocation + ", Last user view: " + lastUserView);

					// Display the new user's location and view
					DisplayUserLocation(true);

					// Send a move to update the Geeo's user location and view
					Geeo_MoveConnectedAgent(lastUserLocation);
					Geeo_MoveConnectedViewport(lastUserView);
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

			// Stop the location service
			if (useSimulatedUserLocation)
				Debug.LogWarning("[DemoScript:StopSimulatedUserLocationUpdate] Simulated user location moves stopped");
			else
			{
				Input.location.Stop();
				Debug.LogWarning("[DemoScript:StopUserLocationUpdate] User location service stopped");
			}

			DisplayStatus(Status.Location, StatusState.Stopped);
		}
		#endregion

		#region User Location And View Display
		// Default depth of user's view display lines
		private const float userViewDisplayLinesDefaultZ = -1f;

		// If the camera must follow the displayed user location
		[SerializeField] private bool userLocationCameraFollowing = true;

		// The main camera
		[SerializeField] private Camera mainCamera;

		// The map on which to display Geeo data
		[SerializeField] private GameObject displayMap;
		[SerializeField] private double displayMapSize = 1024d;

		// The object representing the current user's location
		[SerializeField] private GameObject userLocationDisplayPointPrefab;

		/// <summary>
		/// Display the current user's location point and view bounds.
		/// </summary>
		/// <param name="display">If the user location should be displayed or hidden.</param>
		private void DisplayUserLocation(bool display)
		{
			// If the user location should be displayed, display it and update its position
			if (display)
			{
				// Show the user location point
				lastUserLocation.displayPoint.SetActive(true);

				// Calculate the new user location point's position by converting GPS coordinates to X/Y ones
				float userLocationX, userLocationY;
				LatitudeLongitudeToXY(-lastUserLocation.latitude, lastUserLocation.longitude, out userLocationX, out userLocationY);
				lastUserLocation.displayPoint.transform.localPosition = new Vector3(userLocationX, userLocationY, lastUserLocation.displayPoint.transform.localPosition.z);

				// Calculate the new user view lines' positions by converting GPS coordinates to X/Y ones
				float userViewX1, userViewX2, userViewY1, userViewY2;
				LatitudeLongitudeToXY(-lastUserView.latitude1, lastUserView.longitude1, out userViewX1, out userViewY1);
				LatitudeLongitudeToXY(-lastUserView.latitude2, lastUserView.longitude2, out userViewX2, out userViewY2);
				lastUserView.displayLines.SetPositions(new [] {
					new Vector3(userViewX1, userViewY1, userViewDisplayLinesDefaultZ),
					new Vector3(userViewX2, userViewY1, userViewDisplayLinesDefaultZ),
					new Vector3(userViewX2, userViewY2, userViewDisplayLinesDefaultZ),
					new Vector3(userViewX1, userViewY2, userViewDisplayLinesDefaultZ),
					new Vector3(userViewX1, userViewY1, userViewDisplayLinesDefaultZ)
				});

				// If enabled, set camera's position on top of the new user location
				if (userLocationCameraFollowing)
					mainCamera.transform.position = new Vector3(lastUserLocation.displayPoint.transform.position.x, lastUserLocation.displayPoint.transform.position.y, mainCamera.transform.position.z);
			}
			// If the user location should be hidden and is displayed, hide it
			else
				lastUserLocation.displayPoint.SetActive(false);
		}

		/// <summary>
		/// Convert WGS 84 coordinates to X/Y flat ones (GPS to squared map).
		/// </summary>
		/// <param name="latitude">The input latitude coordinate.</param>
		/// <param name="longitude">The input longitude coordinate.</param>
		/// <param name="x">The output x coordinate.</param>
		/// <param name="y">The output y coordinate.</param>
		private void LatitudeLongitudeToXY(double latitude, double longitude, out float x, out float y)
		{
			latitude = Math.Min(Math.Max(latitude, latitudeMin), latitudeMax);
			longitude = Math.Min(Math.Max(longitude, longitudeMin), longitudeMax);

			double tmpX = (longitude + 180d) / 360d;
			double latitudeSin = Math.Sin(latitude * Math.PI / 180d);
			double tmpY = 0.5d - (Math.Log((1d + latitudeSin) / (1d - latitudeSin)) / (4d * Math.PI));

			x = (float)Math.Min(Math.Max((tmpX * displayMapSize) + 0.5d, 0d), displayMapSize - 1d);
			y = (float)Math.Min(Math.Max((tmpY * displayMapSize) + 0.5d, 0d), displayMapSize - 1d);
		}

		/// <summary>
		/// Convert X/Y flat coordinates to WGS 84 ones (squared map to GPS).
		/// </summary>
		/// <param name="x">The input x coordinate.</param>
		/// <param name="y">The input y coordinate.</param>
		/// <param name="latitude">The output latitude coordinate.</param>
		/// <param name="longitude">The output longitude coordinate.</param>
		private void XYToLatitudeLongitude(float x, float y, out double latitude, out double longitude)
		{
			double tmpX = (Math.Min(Math.Max((double)x, 0d), displayMapSize - 1d) / displayMapSize) - 0.5d;
			double tmpY = 0.5d - (Math.Min(Math.Max((double)y, 0d), displayMapSize - 1d) / displayMapSize);

			latitude = 90d - 360d * Math.Atan(Math.Exp(-tmpY * 2d * Math.PI)) / Math.PI;
			longitude = 360d * tmpX;
		}
		#endregion

		#region Error UI Display
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

		#region Status UI Display
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
