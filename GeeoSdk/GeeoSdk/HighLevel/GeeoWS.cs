﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LitJson;

namespace GeeoSdk
{
	/// <summary>
	/// The SDK's WebSocket module to manage a Websocket connection to a Geeo instance.
	/// </summary>
	public sealed class GeeoWS
	{
		#region Constructor
		// The WebSocket endpoint URL
		private string wsUrl;

		// If the WebSocket connection should be closed when the application focus is lost
		public bool disconnectOnApplicationPause;

		// If regular pings should be sent to check if WebSocket is still alive (should be used for debug purposes only)
		public bool networkCheckPings;

		// The actual WebSocket
		private WebSocket webSocket;

		/// <summary>
		/// WebSocket module's constructor.
		/// </summary>
		/// <param name="_wsUrl">The WebSocket endpoint URL.</param>
		/// <param name="_disconnectOnApplicationPause">If the WebSocket connection should be closed when the application focus is lost.</param>
		/// <param name="_networkCheckPings">If regular pings should be sent to check if WebSocket is still alive. (should be used for debug purposes only)</param>
		/// <param name="_networkCheckDelaySeconds">Delay (in seconds) between two network check pings. (5 seconds minimum, 30 seconds advised)</param>
		public GeeoWS(string _wsUrl, bool _disconnectOnApplicationPause = true, bool _networkCheckPings = false, float _networkCheckDelaySeconds = 30f)
		{
			wsUrl = _wsUrl;
			disconnectOnApplicationPause = _disconnectOnApplicationPause;
			networkCheckPings = _networkCheckPings;

			// Instantiate a specific WebSocket implement depending on which platform the application is running on
			#if WEBGL
			webSocket = new WebSocket_WebGL();
			#else
			webSocket = new WebSocket_CrossPlatform();
			#endif

			_networkCheckDelaySeconds = Mathf.Max(5f, _networkCheckDelaySeconds);
			webSocket.networkCheckDelaySeconds = _networkCheckDelaySeconds;
			webSocket.networkCheckDelayMilliseconds = (int)(_networkCheckDelaySeconds * 1000f);
		}
		#endregion

		#region WebSocket Life Handling
		// The complete URL format for a "websocket connect" request
		private const string webSocketConnect_requestUrlFormat = "{0}?token={1}";

		// The current running WebSocket connection coroutine
		private Coroutine webSocketConnectCoroutine;

		// The last token used to connect with the WebSocket
		private string lastWsToken;

		/// <summary>
		/// Connect the WebSocket with a token previously provided by the Geeo server.
		/// </summary>
		/// <param name="wsToken">The WebSocket token provided by the Geeo server.</param>
		private IEnumerator WebSocketConnect(string wsToken)
		{
			// Build the "websocket connect" request URL
			string requestUrl = string.Format(webSocketConnect_requestUrlFormat, wsUrl, wsToken);
			DebugLogs.LogVerbose("[GeeoWS:WebSocketConnect] Request URL: " + requestUrl);

			// Wait for the connection to be established
			yield return webSocket.Connect(requestUrl);

			// Start the network check (ping)
			if (networkCheckPings)
				webSocket.NetworkCheckStart();

			webSocketConnectCoroutine = null;
		}

		/// <summary>
		/// Close the WebSocket if it is still opened.
		/// </summary>
		private void WebSocketClose()
		{
			// Stop the connect coroutine if still running
			if (webSocketConnectCoroutine != null)
			{
				Geeo.Instance.StopCoroutine(webSocketConnectCoroutine);
				webSocketConnectCoroutine = null;
			}

			// Stop the network check (ping)
			if (networkCheckPings)
				webSocket.NetworkCheckStop();

			// Close the WebSocket
			webSocket.Close();
		}

		/// <summary>
		/// If the user leaves the application and this option is enabled, close the WebSocket to avoid useless data transfers.
		/// When the user comes back, try to connect to the Geeo server again with the last used WebSocket token.
		/// </summary>
		/// <param name="paused">If the application lost the focus.</param>
		internal void OnApplicationPause(bool paused)
		{
			// Check if the "disconnect on application pause" option is enabled
			if (disconnectOnApplicationPause)
			{
				// User leaves the application (close the WebSocket connection but keep the WebSocket token to connect again later)
				if (paused && webSocket.isConnected)
				{
					DebugLogs.LogWarning("[GeeoWS:OnApplicationPause] Application paused ›› Closing connection...");
					WebSocketClose();
				}
				// User resumes the application and there is a stored WebSocket token (try to connect the WebSocket again)
				else if (!paused && !string.IsNullOrEmpty(lastWsToken))
				{
					DebugLogs.LogWarning("[GeeoWS:OnApplicationPause] Application resumed ›› Reopening connection...");
					webSocketConnectCoroutine = Geeo.Instance.StartCoroutine(WebSocketConnect(lastWsToken));
				}
			}
		}

		/// <summary>
		/// Close the WebSocket when application is killed.
		/// </summary>
		internal void OnApplicationQuit()
		{
			DebugLogs.LogWarning("[GeeoWS:OnApplicationQuit] Application quit ›› Closing connection...");
			WebSocketClose();
		}
		#endregion

		#region WebSocket Internal Events
		/// <summary>
		/// Listener to react when a "WebSocket opened" event occurs.
		/// </summary>
		private void OnWebSocketOpen()
		{
			DebugLogs.LogVerbose("[GeeoWS:OnWebSocketOpen] WebSocket opened");

			// Create new Geeo Data instances
			connectedAgent = new Agent();
			connectedViewport = new Viewport();

			// Trigger the OnConnected event if any callback registered to it
			if (OnConnected != null) { OnConnected(); }
		}

		/// <summary>
		/// Listener to react when a "WebSocket closed" event occurs.
		/// </summary>
		private void OnWebSocketClose()
		{
			DebugLogs.LogWarning("[GeeoWS:OnWebSocketClose] WebSocket closed");

			// Reset the Geeo Data instances
			connectedAgent = null;
			connectedViewport = null;
			agents.Clear();
			pointsOfInterest.Clear();

			// Stop the connect coroutine if still running
			if (webSocketConnectCoroutine != null)
			{
				Geeo.Instance.StopCoroutine(webSocketConnectCoroutine);
				webSocketConnectCoroutine = null;
			}

			// Stop the network check (ping)
			if (networkCheckPings)
				webSocket.NetworkCheckStop();

			// Trigger the OnDisconnected event if any callback registered to it
			if (OnDisconnected != null) { OnDisconnected(); }
		}

		/// <summary>
		/// Listener to react when a "WebSocket error" event occurs.
		/// </summary>
		/// <param name="error">The error's message.</param>
		private void OnWebSocketError(string error)
		{
			DebugLogs.LogError("[GeeoWS:OnWebSocketError] WebSocket error ›› " + error);

			// Trigger the OnError event if any callback registered to it
			if (OnError != null) { OnError(error); }
		}

		/// <summary>
		/// Listener to react when a "WebSocket message" event occurs.
		/// </summary>
		/// <param name="message">The received message.</param>
		private void OnWebSocketMessage(string message)
		{
			DebugLogs.LogVerbose("[GeeoWS:OnWebSocketMessage] WebSocket message ›› " + message);

			// Parse Json data from the received WebSocket message from the Geeo server
			JsonData messageJson = JsonMapper.ToObject(message);

			// Check if messageJson is an "error" type object
			if (messageJson.IsObject)
				WebSocketMessage_ErrorReport(JsonMapper.ToObject<ErrorJson>(message));
			// Check if messageJson is an "view update" type array
			else if (messageJson.IsArray)
			{
				// Identify each "update" of the array from the "view update" message
				foreach (JsonData messageUpdate in messageJson)
				{
					// Update type: agent data
					if (messageUpdate.Keys.Contains("agent_id"))
						WebSocketMessage_AgentUpdate(JsonMapper.ToObject<AgentJson>(messageUpdate.ToJson()));
					// Update type: point of interest data
					else if (messageUpdate.Keys.Contains("poi_id"))
						WebSocketMessage_PointOfInterestUpdate(JsonMapper.ToObject<PointOfInterestJson>(messageUpdate.ToJson()));
				}

				// Trigger the OnViewUpdated event if any callback registered to it
				if (OnViewUpdated != null) { OnViewUpdated(); }
			}
		}
		#endregion

		#region WebSocket Messages Handling
		/// <summary>
		/// Handle an "error report" type received WebSocket message from the Geeo server to report an error.
		/// </summary>
		/// <param name="errorData">Received message data about an error.</param>
		private void WebSocketMessage_ErrorReport(ErrorJson errorData)
		{
			DebugLogs.LogError("[GeeoWS:WebSocketMessage_ErrorReport] Server error: " + errorData);

			// Trigger the OnError event if any callback registered to it
			if (OnError != null)
			{
				if (errorData.message != null)
					OnError(errorData.error + " ›› " + errorData.message);
				else
					OnError(errorData.error);
			}
		}

		/// <summary>
		/// Handle an "agent update" type received WebSocket message from the Geeo server to update the agents list.
		/// </summary>
		/// <param name="agentData">Received message data about an agent.</param>
		private void WebSocketMessage_AgentUpdate(AgentJson agentData)
		{
			Agent agent;

			// If the agent just entered the viewport
			if (agentData.entered)
			{
				// If the agent already exists in the agents list, only update this agent's position
				if (agents.ContainsKey(agentData.agent_id))
				{
					DebugLogs.LogWarning("[GeeoWS:WebSocketMessage_AgentUpdate] Agent ‘" + agentData.agent_id + "’ received 2 ‘entered’ updates without any ‘left’ one");

					// Get the agent corresponding to its identifier from the agents list
					agent = agents[agentData.agent_id];

					// Update agent's data from the received message
					agent.latitude = agentData.pos[1];
					agent.longitude = agentData.pos[0];
				}
				// If the agent doesn't exist in the agents list, add this agent to the list
				else
				{
					// Create a new Agent instance and fill its data from the received message
					agent = new Agent();
					agent.id = agentData.agent_id;
					agent.latitude = agentData.pos[1];
					agent.longitude = agentData.pos[0];
					agent.publicData = agentData.publicData;
					
					// Add the new agent to the agents list
					agents.Add(agentData.agent_id, agent);
				}

				// Trigger the OnAgentEntered event if any callback registered to it
				if (OnAgentEntered != null) { OnAgentEntered(agent); }
			}
			// If the agent just left the viewport
			else if (agentData.left)
			{
				// If the agent doesn't exist in the agents list, just ignore this update
				if (!agents.ContainsKey(agentData.agent_id))
				{
					DebugLogs.LogWarning("[GeeoWS:WebSocketMessage_AgentUpdate] Agent ‘" + agentData.agent_id + "’ received a ‘left’ update after another ‘left’ one or no prior ‘entered’ one");

					// Create a new Agent instance and fill its data from the received message
					agent = new Agent();
					agent.id = agentData.agent_id;
				}
				// If the agent does exist in the agents list, remove this agent from the list
				else
				{
					// Get the agent corresponding to its identifier from the agents list
					agent = agents[agentData.agent_id];

					// Remove the agent from the agents list
					agents.Remove(agentData.agent_id);
				}

				// Trigger the OnAgentLeft event if any callback registered to it
				if (OnAgentLeft != null) { OnAgentLeft(agent); }
			}
			// If the agent just moved in the viewport
			else
			{
				// If the agent doesn't exist in the agents list, add this agent to the list
				if (!agents.ContainsKey(agentData.agent_id))
				{
					DebugLogs.LogWarning("[GeeoWS:WebSocketMessage_AgentUpdate] Agent ‘" + agentData.agent_id + "’ received a ‘moved’ update without any prior ‘entered’ one");

					// Create a new Agent instance and fill its data from the received message
					agent = new Agent();
					agent.id = agentData.agent_id;
					agent.latitude = agentData.pos[1];
					agent.longitude = agentData.pos[0];

					// Add the new agent to the agents list
					agents.Add(agentData.agent_id, agent);
				}
				// If the agent does exist in the agents list, update this agent's position
				else
				{
					// Get the agent corresponding to its identifier from the agents list
					agent = agents[agentData.agent_id];

					// Update agent's data from the received message
					agent.latitude = agentData.pos[1];
					agent.longitude = agentData.pos[0];
				}

				// Trigger the OnAgentMoved event if any callback registered to it
				if (OnAgentMoved != null) { OnAgentMoved(agent); }
			}
		}

		/// <summary>
		/// Handle a "point of interest update" type received WebSocket message from the Geeo server to update the points of interest list.
		/// </summary>
		/// <param name="pointOfInterestData">Received message data about a point of interest.</param>
		private void WebSocketMessage_PointOfInterestUpdate(PointOfInterestJson pointOfInterestData)
		{
			PointOfInterest pointOfInterest;

			// If the point of interest just entered the viewport
			if (pointOfInterestData.entered)
			{
				// If the point of interest already exists in the points of interest list, just ignore this update
				if (pointsOfInterest.ContainsKey(pointOfInterestData.poi_id))
				{
					DebugLogs.LogWarning("[GeeoWS:WebSocketMessage_PointOfInterestUpdate] Point of interest ‘" + pointOfInterestData.poi_id + "’ received 2 ‘entered’ updates without any ‘left’ one");

					// Get the point of interest corresponding to its identifier from the points of interest list
					pointOfInterest = pointsOfInterest[pointOfInterestData.poi_id];
				}
				// If the point of interest doesn't exist in the points of interest list, add this point of interest to the list
				else
				{
					// Create a new PointOfInterest instance and fill its data from the received message
					pointOfInterest = new PointOfInterest();
					pointOfInterest.id = pointOfInterestData.poi_id;
					pointOfInterest.latitude = pointOfInterestData.pos[1];
					pointOfInterest.longitude = pointOfInterestData.pos[0];
					pointOfInterest.publicData = pointOfInterestData.publicData;
					pointOfInterest.creatorId = pointOfInterestData.creator;

					// Add the new point of interest to the points of interest list
					pointsOfInterest.Add(pointOfInterestData.poi_id, pointOfInterest);
				}

				// Trigger the OnPointOfInterestEntered event if any callback registered to it
				if (OnPointOfInterestEntered != null) { OnPointOfInterestEntered(pointOfInterest); }
			}
			// If the point of interest just left the viewport
			else if (pointOfInterestData.left)
			{
				// If the point of interest doesn't exist in the points of interest list, just ignore this update
				if (!pointsOfInterest.ContainsKey(pointOfInterestData.poi_id))
				{
					DebugLogs.LogWarning("[GeeoWS:WebSocketMessage_PointOfInterestUpdate] Point of interest ‘" + pointOfInterestData.poi_id + "’ received a ‘left’ update after another ‘left’ one or no prior ‘entered’ one");

					// Create a new PointOfInterest instance and fill its data from the received message
					pointOfInterest = new PointOfInterest();
					pointOfInterest.id = pointOfInterestData.poi_id;
				}
				// If the point of interest does exist in the points of interest list, remove this point of interest from the list
				else
				{
					// Get the point of interest corresponding to its identifier from the points of interest list
					pointOfInterest = pointsOfInterest[pointOfInterestData.poi_id];

					// Remove the point of interest from the points of interest list
					pointsOfInterest.Remove(pointOfInterestData.poi_id);
				}

				// Trigger the OnPointOfInterestLeft event if any callback registered to it
				if (OnPointOfInterestLeft != null) { OnPointOfInterestLeft(pointOfInterest); }
			}
		}
		#endregion

		#region WebSocket Public Events
		/// <summary>
		/// Callback: the WebSocket connected.
		/// </summary>
		public event Action OnConnected;

		/// <summary>
		/// Callback: the WebSocket disconnected.
		/// </summary>
		public event Action OnDisconnected;

		/// <summary>
		/// Callback: an error has been encountered.
		/// </summary>
		public event Action<string> OnError;

		/// <summary>
		/// Callback: an agent entered the viewport.
		/// </summary>
		public event Action<Agent> OnAgentEntered;

		/// <summary>
		/// Callback: an agent left the viewport.
		/// </summary>
		public event Action<Agent> OnAgentLeft;

		/// <summary>
		/// Callback: an agent moved in the viewport.
		/// </summary>
		public event Action<Agent> OnAgentMoved;

		/// <summary>
		/// Callback: a point of interest entered the viewport.
		/// </summary>
		public event Action<PointOfInterest> OnPointOfInterestEntered;

		/// <summary>
		/// Callback: a point of interest left the viewport.
		/// </summary>
		public event Action<PointOfInterest> OnPointOfInterestLeft;

		/// <summary>
		/// Callback: the view has been updated with fresh data (updated agents and points of interest).
		/// This is the right moment to call Agents and PointsOfInterest getters.
		/// </summary>
		public event Action OnViewUpdated;
		#endregion

		#region Geeo Data
		/// <summary>
		/// The currently connected agent (a.k.a. the current client using the Geeo SDK).
		/// </summary>
		public Agent connectedAgent {get; private set;}

		/// <summary>
		/// The currently connected viewport (a.k.a. the view of the current client using the Geeo SDK).
		/// </summary>
		public Viewport connectedViewport {get; private set;}

		/// <summary>
		/// Complete Dictionary (key = agent's id) of all agents currently present in the connected viewport (including the connected agent).
		/// The best moment to call this getter would be when an OnViewUpdated event is triggered.
		/// </summary>
		public Dictionary<string, Agent> agents = new Dictionary<string, Agent>();

		/// <summary>
		/// Complete List of all agents currently present in the connected viewport (including the connected agent).
		/// The best moment to call this getter would be when an OnViewUpdated event is triggered.
		/// </summary>
		public List<Agent> Agents
		{
			// Build a List from the existing Dictionary, then return it
			get
			{
				List<Agent> agentsList = new List<Agent>();

				foreach (KeyValuePair<string, Agent> agent in agents)
					agentsList.Add(agent.Value);

				return agentsList;
			}
		}

		/// <summary>
		/// Complete Dictionary (key = point of interest's id) of all points of interest currently present in the connected viewport.
		/// The best moment to call this getter would be when an OnViewUpdated event is triggered.
		/// </summary>
		public Dictionary<string, PointOfInterest> pointsOfInterest = new Dictionary<string, PointOfInterest>();

		/// <summary>
		/// Complete List of all points of interest currently present in the connected viewport.
		/// The best moment to call this getter would be when an OnViewUpdated event is triggered.
		/// </summary>
		public List<PointOfInterest> PointsOfInterest
		{
			// Build a List from the existing Dictionary, then return it
			get
			{
				List<PointOfInterest> pointsOfInterestList = new List<PointOfInterest>();

				foreach (KeyValuePair<string, PointOfInterest> pointOfInterest in pointsOfInterest)
					pointsOfInterestList.Add(pointOfInterest.Value);

				return pointsOfInterestList;
			}
		}
		#endregion

		#region Requests Handling
		// The Json format for a "move agent" request: { "agentPosition": [longitude, latitude] }
		private const string jsonFormat_moveAgent = "{{\"agentPosition\":[{1},{0}]}}";

		// The Json format for a "move viewport" request: { "viewPosition": [longitude1, latitude1, longitude2, latitude2] }
		private const string jsonFormat_moveViewport = "{{\"viewPosition\":[{2},{0},{3},{1}]}}";

		// The Json format for a "move agent and viewport" request: { "agentPosition": [longitude, latitude], "viewPosition": [longitude1, latitude1, longitude2, latitude2] }
		private const string jsonFormat_moveAgentAndViewport = "{{\"agentPosition\":[{1},{0}],\"viewPosition\":[{4},{2},{5},{3}]}}";

		// The Json format for a "create point of interest" request: { "createPOI": { "poi_id": id, "pos": [longitude, latitude], "publicData": publicData} }
		private const string jsonFormat_createPointOfInterest = "{{\"createPOI\":{{\"poi_id\":\"{0}\",\"pos\":[{2},{1}],\"publicData\":{3}}}}}";

		// The Json format for a "remove point of interest" request: { "removePOI": { "poi_id": id } }
		private const string jsonFormat_removePointOfInterest = "{{\"removePOI\":{{\"poi_id\":\"{0}\"}}}}";

		// The Json format for a "create air beacon" request: { "createAirBeacon": { "ab_id": id, "pos": [longitude1, latitude1, longitude2, latitude2], "publicData": publicData} }
		private const string jsonFormat_createAirBeacon = "{{\"createAirBeacon\":{{\"ab_id\":\"{0}\",\"pos\":[{3},{1},{4},{2}],\"publicData\":{5}}}}}";

		// The Json format for a "remove air beacon" request: { "removeAirBeacon": { "ab_id": id } }
		private const string jsonFormat_removeAirBeacon = "{{\"removeAirBeacon\":{{\"ab_id\":\"{0}\"}}}}";

		/// <summary>
		/// Use a JWT WebSocket token previously provided by the Geeo server to open a WebSocket connection.
		/// If development routes are allowed, you may use the GeeoHTTP.GetGuestToken() method to get a token.
		/// Once the connection is opened, the OnConnected event will be triggered (so you should register a callback to it).
		/// </summary>
		/// <param name="wsToken">The JWT WebSocket token previously provided by the Geeo server.</param>
		public void Connect(string wsToken)
		{
			DebugLogs.LogVerbose("[GeeoWS:Connect] Connecting...");

			// Keep the token used to connect the WebSocket
			lastWsToken = wsToken;

			// Register listeners for future WebSocket events
			webSocket.OnOpen += OnWebSocketOpen;
			webSocket.OnClose += OnWebSocketClose;
			webSocket.OnError += OnWebSocketError;
			webSocket.OnMessage += OnWebSocketMessage;

			// Create a WebSocket and connect to the Geeo server
			webSocketConnectCoroutine = Geeo.Instance.StartCoroutine(WebSocketConnect(wsToken));
		}

		/// <summary>
		/// Remove the last used WebSocket token and registered listeners, then close the current WebSocket connection.
		/// Once the connection is closed, the OnDisconnected event will be triggered (so you should register a callback to it).
		/// </summary>
		public void Disconnect()
		{
			DebugLogs.LogVerbose("[GeeoWS:Disconnect] Disconnecting...");

			// Unvalidate the last used WebSocket token
			lastWsToken = null;

			// Unregister listeners for future WebSocket events
			webSocket.OnOpen -= OnWebSocketOpen;
			webSocket.OnError -= OnWebSocketError;
			webSocket.OnMessage -= OnWebSocketMessage;

			// Disconnect from the Geeo server
			WebSocketClose();

			// Unregister the OnClose listener only after the close call to get this event triggered
			webSocket.OnClose -= OnWebSocketClose;
		}

		/// <summary>
		/// Move the currently connected agent to the specified location.
		/// </summary>
		/// <param name="newLatitude">New agent's location latitude.</param>
		/// <param name="newLongitude">New agent's location longitude.</param>
		public void MoveConnectedAgent(double newLatitude, double newLongitude)
		{
			// Update the local currently connected agent location
			connectedAgent.latitude = newLatitude;
			connectedAgent.longitude = newLongitude;

			// Send a WebSocket message to the Geeo server to update the remote currently connected agent location
			webSocket.Send(string.Format(jsonFormat_moveAgent, newLatitude, newLongitude));
		}

		/// <summary>
		/// Move the currently connected viewport to the specified location.
		/// </summary>
		/// <param name="newLatitude1">New first viewport's latitude bound.</param>
		/// <param name="newLatitude2">New second viewport's latitude bound.</param>
		/// <param name="newLongitude1">New first viewport's longitude bound.</param>
		/// <param name="newLongitude2">New second viewport's longitude bound.</param>
		public void MoveConnectedViewport(double newLatitude1, double newLatitude2, double newLongitude1, double newLongitude2)
		{
			// Update the local currently connected viewport location
			connectedViewport.latitude1 = newLatitude1;
			connectedViewport.latitude2 = newLatitude2;
			connectedViewport.longitude1 = newLongitude1;
			connectedViewport.longitude2 = newLongitude2;

			// Send a WebSocket message to the Geeo server to update the remote currently connected viewport location
			webSocket.Send(string.Format(jsonFormat_moveViewport, newLatitude1, newLatitude2, newLongitude1, newLongitude2));
		}

		/// <summary>
		/// Move the currently connected agent and viewport to the specified locations.
		/// </summary>
		/// <param name="newAgentLatitude">New agent's location latitude.</param>
		/// <param name="newAgentLongitude">New agent's location longitude.</param>
		/// <param name="newViewportLatitude1">New first viewport's latitude bound.</param>
		/// <param name="newViewportLatitude2">New second viewport's latitude bound.</param>
		/// <param name="newViewportLongitude1">New first viewport's longitude bound.</param>
		/// <param name="newViewportLongitude2">New second viewport's longitude bound.</param>
		public void MoveConnectedAgentAndViewport(double newAgentLatitude, double newAgentLongitude, double newViewportLatitude1, double newViewportLatitude2, double newViewportLongitude1, double newViewportLongitude2)
		{
			// Update the local currently connected agent location
			connectedAgent.latitude = newAgentLatitude;
			connectedAgent.longitude = newAgentLongitude;

			// Update the local currently connected viewport location
			connectedViewport.latitude1 = newViewportLatitude1;
			connectedViewport.latitude2 = newViewportLatitude2;
			connectedViewport.longitude1 = newViewportLongitude1;
			connectedViewport.longitude2 = newViewportLongitude2;

			// Send a WebSocket message to the Geeo server to update the remote currently connected agent and viewport locations
			webSocket.Send(string.Format(jsonFormat_moveAgentAndViewport, newAgentLatitude, newAgentLongitude, newViewportLatitude1, newViewportLatitude2, newViewportLongitude1, newViewportLongitude2));
		}

		/// <summary>
		/// The connected agent creates a new point of interest.
		/// </summary>
		/// <param name="id">Point of interest's identifier.</param>
		/// <param name="latitude">Point of interest's location latitude.</param>
		/// <param name="longitude">Point of interest's location longitude.</param>
		/// <param name="publicData">Point of interest's public data.</param>
		public void CreatePointOfInterest(string id, double latitude, double longitude, JsonData publicData = null)
		{
			// Send a WebSocket message to the Geeo server to create the new point of interest
			webSocket.Send(string.Format(jsonFormat_createPointOfInterest, id, latitude, longitude, publicData == null ? "{}" : publicData.ToJson()));
		}

		/// <summary>
		/// The connected agent removes an existing point of interest. (only the creator agent is allowed to do this)
		/// </summary>
		/// <param name="id">Point of interest's identifier.</param>
		public void RemovePointOfInterest(string id)
		{
			// Send a WebSocket message to the Geeo server to remove the existing point of interest
			webSocket.Send(string.Format(jsonFormat_removePointOfInterest, id));
		}

		/// <summary>
		/// The connected agent creates a new air beacon.
		/// </summary>
		/// <param name="id">Air beacon's identifier.</param>
		/// <param name="latitude1">Air beacon's first latitude bound.</param>
		/// <param name="latitude2">Air beacon's second latitude bound.</param>
		/// <param name="longitude1">Air beacon's first longitude bound.</param>
		/// <param name="longitude2">Air beacon's second longitude bound.</param>
		/// <param name="publicData">Air beacon's public data.</param>
		public void CreateAirBeacon(string id, double latitude1, double latitude2, double longitude1, double longitude2, JsonData publicData = null)
		{
			// Send a WebSocket message to the Geeo server to create the new air beacon
			webSocket.Send(string.Format(jsonFormat_createAirBeacon, id, latitude1, latitude2, longitude1, longitude2, publicData == null ? "{}" : publicData.ToJson()));
		}

		/// <summary>
		/// The connected agent removes an existing air beacon. (only the creator agent is allowed to do this)
		/// </summary>
		/// <param name="id">Air beacon's identifier.</param>
		public void RemoveAirBeacon(string id)
		{
			// Send a WebSocket message to the Geeo server to remove the existing air beacon
			webSocket.Send(string.Format(jsonFormat_removeAirBeacon, id));
		}
		#endregion
	}
}
