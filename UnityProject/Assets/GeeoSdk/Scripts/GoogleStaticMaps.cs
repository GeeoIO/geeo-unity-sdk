using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeeoDemo
{
	/// <summary>
	/// A sample script to demonstrate how to use Google Static Maps APIs with Geeo SDK's features.
	/// </summary>
	public class GoogleStaticMaps : MonoBehaviour
	{
		#region Class Definitions
		/// <summary>
		/// Represents a group of markers with its associated style and list of markers to display on the map.
		/// </summary>
		public class MarkersGroup
		{
			// Locations represented by map markers
			public List<Location> locations;

			// Color of the map markers
			public MarkerColor markersColor;

			// Size of the map markers
			public MarkerSize markersSize;

			// Label of the map markers
			public char markersLabel;

			/// <summary>
			/// MarkersGroup class constructor.
			/// </summary>
			/// <param name="_locations">Locations represented by map markers.</param>
			/// <param name="_markersColor">Color of the map markers.</param>
			/// <param name="_markersSize">Size of the map markers.</param>
			/// <param name="_markersLabel">Label of the map markers. Only {A-Z, 0-9} uppercase chars are allowed.</param>
			public MarkersGroup(List<Location> _locations, MarkerColor _markersColor, MarkerSize _markersSize, char _markersLabel)
			{
				locations = _locations;
				markersColor = _markersColor;
				markersSize = _markersSize;
				markersLabel = _markersLabel;
			}

			/// <summary>
			/// Converts the value of this instance to its equivalent string representation.
			/// {locations.Count, markersColor, markersSize, markersLabel}
			/// </summary>
			public override string ToString()
			{
				return string.Format("{{ Locations: {0}, Color: {1}, Size: {2}, Label: {3} }}", locations.Count, markersColor, markersSize, markersLabel);
			}
		}

		/// <summary>
		/// Represents a location with its latitude and longitude coordinates.
		/// </summary>
		public class Location
		{
			// Location coordinates
			public double latitude;
			public double longitude;

			/// <summary>
			/// Location class constructor.
			/// </summary>
			/// <param name="_latitude">Location latitude.</param>
			/// <param name="_longitude">Location longitude.</param>
			public Location(double _latitude, double _longitude)
			{
				latitude = _latitude;
				longitude = _longitude;
			}

			/// <summary>
			/// Converts the value of this instance to its equivalent string representation.
			/// {latitude, longitude}
			/// </summary>
			public override string ToString()
			{
				return string.Format("{{ La: {0}, Lo: {1} }}", latitude, longitude);
			}
		}
		#endregion

		#region URL Parameters
		// Static Maps APIs URL
		private const string apiUrl = "https://maps.googleapis.com/maps/api/staticmap";

		// Center parameter format: center=[latitude],[longitude]
		private const string centerParameterFormat = "center={0},{1}";

		// MapType parameter format: mapType=[mapType]
		private const string mapTypeParameterFormat = "maptype={0}";

		// Size parameter format: size=[mapWidth]x[mapHeight]
		private const string sizeParameterFormat = "size={0}x{1}";

		// Zoom parameter format: zoom=[zoomLevel]
		private const string zoomParameterFormat = "zoom={0}";

		// Markers parameter format: markers=[parameters]
		private const string markersParameterFormat = "markers={0}";

		// Key parameter format: key=[apiKey]
		private const string keyParameterFormat = "key={0}";

		/// <summary>
		/// Build the entire URL to send to get a map from Google Static Maps according to the given parameters.
		/// </summary>
		/// <param name="centerLatitude">Map's center latitude.</param>
		/// <param name="centerLongitude">Map's center longitude.</param>
		/// <param name="mapMarkersGroups">Groups of markers to show on the map.</param>
		private string BuildMapUrl(double centerLatitude, double centerLongitude, params MarkersGroup[] mapMarkersGroups)
		{
			string urlParameters = "";

			// Add the "center" parameter: coordinates of the center view point of the requested map
			urlParameters += string.Format(centerParameterFormat, centerLatitude, centerLongitude);

			// Add the "mapType" parameter: type of map to render
			urlParameters += "&" + string.Format(mapTypeParameterFormat, mapType.ToString().ToLower());

			// Add the "size" parameter: number of pixels to get
			urlParameters += "&" + string.Format(sizeParameterFormat, mapWidth, mapHeight);

			// Add the "zoom" parameter: level of details
			urlParameters += "&" + string.Format(zoomParameterFormat, zoomLevel);

			// Add the "markers" parameter(s): map markers representing agents and points of interest locations
			foreach (MarkersGroup mapMarkersGroup in mapMarkersGroups)
			{
				// Set the markers label
				string markersParameters = "label:" + mapMarkersGroup.markersLabel;

				// Set the markers color
				if (mapMarkersGroup.markersColor != MarkerColor.Default)
					markersParameters += "|color:" + mapMarkersGroup.markersColor.ToString().ToLower();

				// Set the markers size
				if (mapMarkersGroup.markersSize != MarkerSize.Default)
					markersParameters += "|size:" + mapMarkersGroup.markersSize.ToString().ToLower();

				// Set the markers locations
				foreach (Location location in mapMarkersGroup.locations)
					markersParameters += "|" + location.latitude + "," + location.longitude;

				urlParameters += "&" + string.Format(markersParameterFormat, markersParameters);
			}

			// If not empty, add the "key" parameter: application's Google Static Maps identifier
			if (!string.IsNullOrEmpty(apiKey))
				urlParameters += "&" + string.Format(keyParameterFormat, apiKey);

			// Return the final built URL with all parameters
			return apiUrl + "?" + urlParameters;
		}
		#endregion

		#region Map Display
		// List of available map types
		public enum MapType {Hybrid, RoadMap, Satellite, Terrain}

		// List of available marker colors
		public enum MarkerColor {Default, Black, Blue, Brown, Gray, Green, Orange, Purple, Red, White, Yellow}

		// List of available marker sizes
		public enum MarkerSize {Default, Mid, Small, Tiny}

		// Callback: the map display just refreshed
		public event Action OnMapRefreshed;

		// GPS coordinates constants
		private const double latitudeMin = -85.05112878d;
		private const double latitudeMax = 85.05112878d;
		private const double longitudeMin = -180d;
		private const double longitudeMax = 180d;

		// The allowed zoom level bounds
		private const int zoomLevelMin = 0;
		private const int zoomLevelMax = 21;

		// Renderer to display the map
		[SerializeField] private Renderer mapRenderer;

		// The Google Static Maps API key to associate to map requests
		[SerializeField] private string apiKey = "";

		// Type of map to render
		[SerializeField] private MapType mapType = MapType.RoadMap;

		// Number of pixels to render the map
		[SerializeField] [Range(128f, 1024f)] private int mapHeight = 310;
		[SerializeField] [Range(128f, 1024f)] private int mapWidth = 512;

		// Zoom level to define the map level of details
		[SerializeField] [Range((float)zoomLevelMin, (float)zoomLevelMax)] private int zoomLevel = 14;

		// The last latitude and longitude used to get a map
		private double lastCenterLatitude = 0d;
		private double lastCenterLongitude = 0d;

		/// <summary>
		/// The last latitude used to get a map.
		/// </summary>
		public double CenterLatitude { get {return lastCenterLatitude;} }

		/// <summary>
		/// The last longitude used to get a map.
		/// </summary>
		public double CenterLongitude { get {return lastCenterLongitude;} }

		/// <summary>
		/// Zoom level to define the map level of details.
		/// </summary>
		public int ZoomLevel { get {return zoomLevel;} }

		/// <summary>
		/// If the maximum zoom level is not reached yet, increase it by 1.
		/// Return the actual zoom level in allowed bounds.
		/// </summary>
		public int ZoomIn()
		{
			// If the maximum zoom level is not reached yet, increase it by 1
			if (zoomLevel < zoomLevelMax)
				++zoomLevel;

			// Return the actual zoom level
			return zoomLevel;
		}

		/// <summary>
		/// If the minimum zoom level is not reached yet, decrease it by 1.
		/// Return the actual zoom level in allowed bounds.
		/// </summary>
		public int ZoomOut()
		{
			// If the maximum zoom level is not reached yet, increase it by 1
			if (zoomLevel > zoomLevelMin)
				--zoomLevel;

			// Return the actual zoom level
			return zoomLevel;
		}

		/// <summary>
		/// Get and display a new map from Google Static Maps according to the given parameters.
		/// </summary>
		/// <param name="centerLatitude">Map's center latitude.</param>
		/// <param name="centerLongitude">Map's center longitude.</param>
		/// <param name="mapMarkersGroups">Groups of markers to show on the map.</param>
		public void RefreshMapDisplay(double centerLatitude = double.NaN, double centerLongitude = double.NaN, params MarkersGroup[] mapMarkersGroups)
		{
			// Clip latitude and longitude coordinates into allowed GPS coordinates bounds
			if (!double.IsNaN(centerLatitude) && !double.IsNaN(centerLongitude))
			{
				lastCenterLatitude = centerLatitude = Math.Min(Math.Max(centerLatitude, latitudeMin), latitudeMax);
				lastCenterLongitude = centerLongitude = Math.Min(Math.Max(centerLongitude, longitudeMin), longitudeMax);
			}

			// Build the map request URL
			string requestUrl = BuildMapUrl(lastCenterLatitude, lastCenterLongitude, mapMarkersGroups);

			// Start the map request
			StartCoroutine(RefreshMapDisplay_Coroutine(requestUrl));
		}

		/// <summary>
		/// Coroutine to get and display a new map from Google Static Maps according to the given parameters.
		/// </summary>
		/// <param name="requestUrl">The URL to request a map.</param>
		private IEnumerator RefreshMapDisplay_Coroutine(string requestUrl)
		{
			Debug.Log("[GoogleStaticMaps:RefreshMapDisplay_Coroutine] Request URL: " + requestUrl);

			// Make the map request and wait for the answer
			WWW request = new WWW(requestUrl);
			yield return request;

			// Display the obtained map on a texture
			mapRenderer.material.mainTexture = request.texture;

			// Trigger the OnMapRefreshed event if any callback registered to it
			if (OnMapRefreshed != null)
				OnMapRefreshed();
		}
		#endregion
	}
}
