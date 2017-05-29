using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using GeeoSdk;

namespace GeeoDemo
{
	/// <summary>
	/// A sample script to demonstrate how to use the Geeo SDK's features. Needs to be put as a component of an active object in the scene.
	/// This sample uses Google Static Maps to allow zoom in/out and nice levels of details for a fancy display.
	/// This sample uses the OnViewUpdated events to periodically get then display all agents and points of interest stored on the Geeo SDK side.
	/// </summary>
	public class DemoScript2 : MonoBehaviour
	{
		#region Geeo SDK Initialization
		// Format to get an ISO 8601 DateTime string
		private const string dateTimeFormat_Iso8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";

		/// <summary>
		/// At Start, initialize the Geeo SDK.
		/// </summary>
		private void Start()
		{
			// Disable map control buttons
			EnableMapButtons(false);

			// Disable point of interest creation button
			pointOfInterestCreationButton.interactable = false;

			// Bind the OnMapRefreshed callback with the OnMapRefreshed Google Static Maps event
			googleMap.OnMapRefreshed += GoogleMap_OnMapRefreshed;

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
			string currentAgentId = "agent" + currentDateTime;
			string currentViewportId = "view" + currentDateTime;

			// Set the default user location in allowed GPS bounds
			lastUserLocation = new UserLocation(currentAgentId, defaultUserLocationLatitude, defaultUserLocationLongitude);

			// Set the default user view
			lastUserView = new UserView(currentViewportId, Math.Min(Math.Max(defaultUserLocationLatitude - userViewExtentLatitude, latitudeMin), latitudeMax),
				Math.Min(Math.Max(defaultUserLocationLatitude + userViewExtentLatitude, latitudeMin), latitudeMax),
				Math.Min(Math.Max(defaultUserLocationLongitude - userViewExtentLongitude, longitudeMin), longitudeMax),
				Math.Min(Math.Max(defaultUserLocationLongitude + userViewExtentLongitude, longitudeMin), longitudeMax));

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

			// To get started, send a move with the default user location and view
			Geeo_MoveConnectedAgent(lastUserLocation);
			Geeo_MoveConnectedViewport(lastUserView);

			// Update the map display
			DisplayMap(double.NaN, double.NaN);

			// Start the location service to get the user location (or use the simulated user location if enabled)
			runningUserLocationUpdateCoroutine = StartCoroutine(useSimulatedUserLocation ? StartSimulatedUserLocationUpdate() : StartUserLocationUpdate());

			// Enable point of interest creation button
			pointOfInterestCreationButton.interactable = true;
		}

		/// <summary>
		/// Callback: the Geeo SDK just disconnected.
		/// </summary>
		private void Geeo_OnDisconnected()
		{
			Debug.LogWarning("[DemoScript:Geeo_OnDisconnected] Geeo disconnected");
			DisplayStatus(Status.Geeo, StatusState.Stopped);

			// Stop the location service (no need to get the user location anymore)
			StopUserLocationUpdate();

			// Disable map control buttons
			EnableMapButtons(false);

			// Disable point of interest creation button
			pointOfInterestCreationButton.interactable = false;
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
		// The point of interest creation UI elements
		[SerializeField] private Button pointOfInterestCreationButton;
		[SerializeField] private InputField pointOfInterestCreationLatitude;
		[SerializeField] private InputField pointOfInterestCreationLongitude;

		/// <summary>
		/// Update the Geeo's connected agent location.
		/// </summary>
		/// <param name="currentAgentLocation">The new last known user agent location.</param>
		private void Geeo_MoveConnectedAgent(UserLocation currentAgentLocation)
		{
			// Call the Geeo SDK's API to move the connected agent (user location)
			Geeo.Instance.ws.MoveConnectedAgent(currentAgentLocation.latitude, currentAgentLocation.longitude);
		}

		/// <summary>
		/// Update the Geeo's connected viewport location.
		/// </summary>
		/// <param name="currentViewportLocation">The new last known user view location.</param>
		private void Geeo_MoveConnectedViewport(UserView currentViewportLocation)
		{
			// Call the Geeo SDK's API to move the connected viewport (user view)
			Geeo.Instance.ws.MoveConnectedViewport(currentViewportLocation.latitude1, currentViewportLocation.latitude2, currentViewportLocation.longitude1, currentViewportLocation.longitude2);
		}

		/// <summary>
		/// Create a point of interest.
		/// </summary>
		public void Button_CreatePointOfInterest()
		{
			// Create a point of interest to the given coordinates
			if (!string.IsNullOrEmpty(pointOfInterestCreationLatitude.text) && !string.IsNullOrEmpty(pointOfInterestCreationLongitude.text))
			{
				// Generate pseudo-random point of interest identifier
				string pointOfInterestId = "poi" + DateTime.UtcNow.ToString(dateTimeFormat_Iso8601);

				// Check the new point of interest coordinates
				double latitude = Math.Min(Math.Max(double.Parse(pointOfInterestCreationLatitude.text), latitudeMin), latitudeMax);
				double longitude = Math.Min(Math.Max(double.Parse(pointOfInterestCreationLongitude.text), latitudeMin), latitudeMax);

				// Empty the point of interest creation input fields
				pointOfInterestCreationLatitude.text = "";
				pointOfInterestCreationLongitude.text = "";

				// Call the Geeo SDK's API to create a new point of interest
				Geeo.Instance.ws.CreatePointOfInterest(pointOfInterestId, latitude, longitude);

				// Update the map display
				DisplayMap(double.NaN, double.NaN);
			}
		}
		#endregion

		#region User Location & View Update
		/// <summary>
		/// Represents a user point location.
		/// </summary>
		private class UserLocation
		{
			// User point location's identifier
			public string id;

			// User point location's coordinates
			public double latitude;
			public double longitude;

			/// <summary>
			/// UserLocation class constructor.
			/// </summary>
			/// <param name="_id">User point's location identifier.</param>
			/// <param name="_latitude">User point's location latitude.</param>
			/// <param name="_longitude">User point's location longitude.</param>
			public UserLocation(string _id, double _latitude, double _longitude)
			{
				id = _id;
				latitude = _latitude;
				longitude = _longitude;
			}

			/// <summary>
			/// Converts the value of this instance to its equivalent string representation.
			/// {id, latitude, longitude}
			/// </summary>
			public override string ToString()
			{
				return string.Format("{{ Id: {0}, La: {1}, Lo: {2} }}", id, latitude, longitude);
			}
		}

		/// <summary>
		/// Represents a user square view.
		/// </summary>
		private class UserView
		{
			// User square view's identifier
			public string id;

			// User square view's coordinates
			public double latitude1;
			public double latitude2;
			public double longitude1;
			public double longitude2;

			/// <summary>
			/// UserView class constructor.
			/// </summary>
			/// <param name="_id">User square's view identifier.</param>
			/// <param name="_latitude1">First user square's view latitude bound.</param>
			/// <param name="_latitude2">Second user square's view latitude bound.</param>
			/// <param name="_longitude1">First user square's view longitude bound.</param>
			/// <param name="_longitude2">Second user square's view longitude bound.</param>
			public UserView(string _id, double _latitude1, double _latitude2, double _longitude1, double _longitude2)
			{
				id = _id;
				latitude1 = _latitude1;
				latitude2 = _latitude2;
				longitude1 = _longitude1;
				longitude2 = _longitude2;
			}

			/// <summary>
			/// Converts the value of this instance to its equivalent string representation.
			/// {id, latitude1, latitude2, longitude1, longitude2}
			/// </summary>
			public override string ToString()
			{
				return string.Format("{{ Id: {0}, La1: {1}, La2: {2}, Lo1: {3}, Lo2: {4} }}", id, latitude1, latitude2, longitude1, longitude2);
			}
		}

		// Time to wait (in seconds) between each location service initialization check
		private const float locationServiceInitChecksDelay = 1f;

		// GPS coordinates constants
		private const double latitudeMin = -85.05112878d;
		private const double latitudeMax = 85.05112878d;
		private const double longitudeMin = -180d;
		private const double longitudeMax = 180d;

		// Instance of the Google Static Maps API's implement
		[SerializeField] private GoogleStaticMaps googleMap;

		// Time to wait (in seconds) between each update of the user location from the location service
		[SerializeField] [Range(5f, 600f)] private float userLocationUpdatesDelay = 10f;

		// If continuous user locations should be simulated instead of using the location service
		[SerializeField] private bool useSimulatedUserLocation = false;

		// Range of maximum allowed simulated moves on latitude and longitude per update
		[SerializeField] [Range(0f, 10f)] private float simulatedUserLocationMoveRange = 1f;

		// As long as no user location can be obtained from the location service, let's say you're in Tenerife by default
		[SerializeField] [Range((float)latitudeMin, (float)latitudeMax)] private double defaultUserLocationLatitude = 28.0479823d;
		[SerializeField] [Range((float)longitudeMin, (float)longitudeMax)] private double defaultUserLocationLongitude = -16.7173771d;

		// How much X/Y to add/subtract to user's location to get its view bounds
		[SerializeField] [Range(0.0001f, (float)latitudeMax)] private double userViewExtentLatitude = 10d;
		[SerializeField] [Range(0.0001f, (float)longitudeMax)] private double userViewExtentLongitude = 20d;

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

				// Update the last user location data in allowed GPS bounds
				lastUserLocation.latitude = Math.Min(Math.Max(lastUserLocation.latitude + (double)UnityEngine.Random.Range(-simulatedUserLocationMoveRange, simulatedUserLocationMoveRange), latitudeMin), latitudeMax);
				lastUserLocation.longitude = Math.Min(Math.Max(lastUserLocation.longitude + (double)UnityEngine.Random.Range(-simulatedUserLocationMoveRange, simulatedUserLocationMoveRange), longitudeMin), longitudeMax);

				// Calculate the current user view, based on its new location
				UserViewUpdate();

				Debug.Log("[DemoScript:StartSimulatedUserLocationUpdate] Last user location: " + lastUserLocation + ", Last user view: " + lastUserView);

				// Send a move to update the Geeo's user location and view
				Geeo_MoveConnectedAgent(lastUserLocation);
				Geeo_MoveConnectedViewport(lastUserView);

				// Update the map display
				DisplayMap(double.NaN, double.NaN);
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

					// Calculate the current user view, based on its new location
					UserViewUpdate();

					Debug.Log("[DemoScript:StartUserLocationUpdate] Last user location: " + lastUserLocation + ", Last user view: " + lastUserView);

					// Send a move to update the Geeo's user location and view
					Geeo_MoveConnectedAgent(lastUserLocation);
					Geeo_MoveConnectedViewport(lastUserView);

					// Update the map display
					DisplayMap(double.NaN, double.NaN);
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

		/// <summary>
		/// Update current user view, based on its location.
		/// </summary>
		private void UserViewUpdate()
		{
			// Ensure the updated view bounds are in allowed GPS coordinates
			lastUserView.latitude1 = Math.Min(Math.Max(lastUserLocation.latitude - userViewExtentLatitude, latitudeMin), latitudeMax);
			lastUserView.latitude2 = Math.Min(Math.Max(lastUserLocation.latitude + userViewExtentLatitude, latitudeMin), latitudeMax);
			lastUserView.longitude1 = Math.Min(Math.Max(lastUserLocation.longitude - userViewExtentLongitude, longitudeMin), longitudeMax);
			lastUserView.longitude2 = Math.Min(Math.Max(lastUserLocation.longitude + userViewExtentLongitude, longitudeMin), longitudeMax);
		}
		#endregion

		#region Error UI Display
		// Error display UI elements and parameters
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

		#region Map UI Display
		// The main camera
		[SerializeField] private Camera mainCamera;

		// How much matitude/longitude to add/subtract when moving the map center location
		[SerializeField] [Range(10f, 500f)] private double mapCenterLatitudeMoveRange = 200d;
		[SerializeField] [Range(20f, 1000f)] private double mapCenterLongitudeMoveRange = 400d;

		// How much markers are allowed to be displayed on the map
		[SerializeField] private int mapMarkersMax = 200;

		// The map display UI elements
		[SerializeField] private Button mapZoomInButton;
		[SerializeField] private Button mapZoomOutButton;
		[SerializeField] private Button mapCenterToNorthButton;
		[SerializeField] private Button mapCenterToEastButton;
		[SerializeField] private Button mapCenterToSouthButton;
		[SerializeField] private Button mapCenterToWestButton;

		// OnViewUpdated delegate
		Action onViewUpdatedDelegate = null;

		/// <summary>
		/// Update the map display at the next OnViewUpdated Geeo event to get the latest agents and points of interest data.
		/// Use lastUserLocation coordinates if none are provided.
		/// </summary>
		/// <param name="centerLatitude">Map's center latitude. (if NaN, use lastUserLocation latitude)</param>
		/// <param name="centerLongitude">Map's center longitude. (if NaN, use lastUserLocation longitude)</param>
		private void DisplayMap(double centerLatitude = double.NaN, double centerLongitude = double.NaN)
		{
			// Disable map control buttons
			EnableMapButtons(false);

			// Define the OnViewUpdated delegate
			onViewUpdatedDelegate = delegate()
			{
				int mapMarkersCount = 1;

				// Unregister from the OnViewUpdated event
				Geeo.Instance.ws.OnViewUpdated -= onViewUpdatedDelegate;

				// Define the user map marker
				List<GoogleStaticMaps.Location> userMarkerLocation = new List<GoogleStaticMaps.Location>();
				userMarkerLocation.Add(new GoogleStaticMaps.Location(lastUserLocation.latitude, lastUserLocation.longitude));

				GoogleStaticMaps.MarkersGroup userMarker = new GoogleStaticMaps.MarkersGroup(userMarkerLocation, GoogleStaticMaps.MarkerColor.Green, GoogleStaticMaps.MarkerSize.Default, 'U');

				// Define the agents map markers
				List<GoogleStaticMaps.Location> agentsMarkersLocations = new List<GoogleStaticMaps.Location>();

				foreach (Agent agent in Geeo.Instance.ws.Agents)
				{
					// Ensure to set no more markers than the allowed count
					if (mapMarkersCount >= mapMarkersMax)
						break;
					
					agentsMarkersLocations.Add(new GoogleStaticMaps.Location(agent.latitude, agent.longitude));
					++mapMarkersCount;
				}

				GoogleStaticMaps.MarkersGroup agentsMarkers = new GoogleStaticMaps.MarkersGroup(agentsMarkersLocations, GoogleStaticMaps.MarkerColor.Yellow, GoogleStaticMaps.MarkerSize.Mid, 'A');

				// Define the points of interest map markers
				List<GoogleStaticMaps.Location> pointsOfInterestMarkersLocations = new List<GoogleStaticMaps.Location>();

				foreach (PointOfInterest pointOfInterest in Geeo.Instance.ws.PointsOfInterest)
				{
					// Ensure to set no more markers than the allowed count
					if (mapMarkersCount >= mapMarkersMax)
						break;

					pointsOfInterestMarkersLocations.Add(new GoogleStaticMaps.Location(pointOfInterest.latitude, pointOfInterest.longitude));
					++mapMarkersCount;
				}

				GoogleStaticMaps.MarkersGroup pointsOfInterestMarkers = new GoogleStaticMaps.MarkersGroup(pointsOfInterestMarkersLocations, GoogleStaticMaps.MarkerColor.Blue, GoogleStaticMaps.MarkerSize.Mid, 'P');

				// Display given coordinates' surrounding map
				if (!double.IsNaN(centerLatitude) && !double.IsNaN(centerLongitude))
					googleMap.RefreshMapDisplay(centerLatitude, centerLongitude, userMarker, agentsMarkers, pointsOfInterestMarkers);
				// Display user location's surrounding map
				else
					googleMap.RefreshMapDisplay(lastUserLocation.latitude, lastUserLocation.longitude, userMarker, agentsMarkers, pointsOfInterestMarkers);
			};

			// Register to the OnViewUpdated event to wait for the next Geeo SDK's agents and points of interest update
			Geeo.Instance.ws.OnViewUpdated += onViewUpdatedDelegate;
		}

		/// <summary>
		/// Callback: the Geeo SDK just disconnected.
		/// </summary>
		private void GoogleMap_OnMapRefreshed()
		{
			// Enable map control buttons
			EnableMapButtons(true);
		}

		/// <summary>
		/// Enable or disable all map UI buttons.
		/// </summary>
		/// <param name="enabled">If the buttons should be enabled.</param>
		private void EnableMapButtons(bool enabled)
		{
			mapZoomInButton.interactable = enabled;
			mapZoomOutButton.interactable = enabled;
			mapCenterToNorthButton.interactable = enabled;
			mapCenterToEastButton.interactable = enabled;
			mapCenterToSouthButton.interactable = enabled;
			mapCenterToWestButton.interactable = enabled;
		}

		/// <summary>
		/// When the corresponding button is clicked, zoom the display map in by 1 step.
		/// </summary>
		public void Button_MapZoomIn()
		{
			int currentZoom = googleMap.ZoomLevel;

			// Zoom the display map in by 1 step
			if (googleMap.ZoomIn() != currentZoom)
				DisplayMap(googleMap.CenterLatitude, googleMap.CenterLongitude);
		}

		/// <summary>
		/// When the corresponding button is clicked, zoom the display map out by 1 step.
		/// </summary>
		public void Button_MapZoomOut()
		{
			int currentZoom = googleMap.ZoomLevel;

			// Zoom the display map out by 1 step
			if (googleMap.ZoomOut() != currentZoom)
				DisplayMap(googleMap.CenterLatitude, googleMap.CenterLongitude);
		}

		/// <summary>
		/// When the corresponding button is clicked, move the map center location to the north.
		/// </summary>
		public void Button_MapCenterToNorth()
		{
			// Move the map center location to the north
			DisplayMap(googleMap.CenterLatitude + (mapCenterLatitudeMoveRange / Math.Pow(2, googleMap.ZoomLevel)), googleMap.CenterLongitude);
		}

		/// <summary>
		/// When the corresponding button is clicked, move the map center location to the east.
		/// </summary>
		public void Button_MapCenterToEast()
		{
			// Move the map center location to the east
			DisplayMap(googleMap.CenterLatitude, googleMap.CenterLongitude + (mapCenterLongitudeMoveRange / Math.Pow(2, googleMap.ZoomLevel)));
		}

		/// <summary>
		/// When the corresponding button is clicked, move the map center location to the south.
		/// </summary>
		public void Button_MapCenterToSouth()
		{
			// Move the map center location to the south
			DisplayMap(googleMap.CenterLatitude - (mapCenterLatitudeMoveRange / Math.Pow(2, googleMap.ZoomLevel)), googleMap.CenterLongitude);
		}

		/// <summary>
		/// When the corresponding button is clicked, move the map center location to the west.
		/// </summary>
		public void Button_MapCenterToWest()
		{
			// Move the map center location to the west
			DisplayMap(googleMap.CenterLatitude, googleMap.CenterLongitude - (mapCenterLongitudeMoveRange / Math.Pow(2, googleMap.ZoomLevel)));
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
