using System;
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
		private bool disconnectOnApplicationPause;

		// The actual WebSocket
		private WebSocket webSocket;

		/// <summary>
		/// WebSocket module's constructor.
		/// </summary>
		/// <param name="_wsUrl">The WebSocket endpoint URL.</param>
		/// <param name="_disconnectOnApplicationPause">If the WebSocket connection should be closed when the application focus is lost.</param>
		public GeeoWS(string _wsUrl, bool _disconnectOnApplicationPause = true)
		{
			wsUrl = _wsUrl;
			disconnectOnApplicationPause = _disconnectOnApplicationPause;

			// Instantiate a specific WebSocket implement depending on which platform the application is running on
			switch (Application.platform)
			{
				case RuntimePlatform.WebGLPlayer:
				webSocket = new WebSocket_WebGL();
				break;

				default:
				webSocket = new WebSocket_CrossPlatform();
				break;
			}
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

			// Call the OnConnected callback
			OnConnected();
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

			// Call the OnDisconnected callback
			OnDisconnected();
		}

		/// <summary>
		/// Listener to react when a "WebSocket error" event occurs.
		/// </summary>
		/// <param name="error">The error's message.</param>
		private void OnWebSocketError(string error)
		{
			DebugLogs.LogError("[GeeoWS:OnWebSocketError] WebSocket error ›› " + error);

			// Call the OnError callback
			OnError(error);
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

			// TODO: Check if messageJson is an error object

			// Identify each "event" of the array from the message
			foreach (JsonData messageEvent in messageJson)
			{
				// Event type: agent data
				if (messageEvent.Keys.Contains("agent_id"))
					WebSocketMessage_AgentEvent(JsonMapper.ToObject<AgentJson>(messageEvent.ToJson()));
			}
		}
		#endregion

		#region WebSocket Messages Handling
		/// <summary>
		/// Handle an "agent" type received WebSocket message from the Geeo server to update the agents list.
		/// </summary>
		/// <param name="agentData">Received message data about an agent.</param>
		private void WebSocketMessage_AgentEvent(AgentJson agentData)
		{
			// If the agent just entered the viewport
			if (agentData.entered)
			{
				// Ensure the agent doesn't exist yet in the agents list
				if (agents.ContainsKey(agentData.agent_id))
				{
					DebugLogs.LogError("[GeeoWS:WebSocketMessage_AgentEvent] A new agent entered the viewport ›› Agent identifier ‘" + agentData.agent_id + "’ already exists");
					OnError("A new agent entered the viewport ›› Agent identifier ‘" + agentData.agent_id + "’ already exists");
					return;
				}

				// Create a new Agent instance and fill its data from the received message
				Agent agent = new Agent();
				agent.id = agentData.agent_id;
				agent.latitude = agentData.pos[1];
				agent.longitude = agentData.pos[0];
				agent.publicData = agentData.publicData;
				
				// Add the new agent to the agents list
				agents.Add(agentData.agent_id, agent);

				// Trigger the "agent just entered the viewport" event callback
				OnAgentEntered(agent);
			}
			// If the agent just left the viewport
			else if (agentData.left)
			{
				// Ensure the agent does exist in the agents list
				if (!agents.ContainsKey(agentData.agent_id))
				{
					DebugLogs.LogError("[GeeoWS:WebSocketMessage_AgentEvent] An agent left the viewport ›› Agent identifier ‘" + agentData.agent_id + "’ does not exist");
					OnError("An agent left the viewport ›› Agent identifier ‘" + agentData.agent_id + "’ does not exist");
					return;
				}

				// Get the agent corresponding to its identifier for the agents list
				Agent agent = agents[agentData.agent_id];

				// Remove the agent from the agents list
				agents.Remove(agentData.agent_id);

				// Trigger the "agent just left the viewport" event callback
				OnAgentLeft(agent);
			}
			// If the agent just moved in the viewport
			else
			{
				// Ensure the agent does exist in the agents list
				if (!agents.ContainsKey(agentData.agent_id))
				{
					DebugLogs.LogError("[GeeoWS:WebSocketMessage_AgentEvent] An agent moved in the viewport ›› Agent identifier ‘" + agentData.agent_id + "’ does not exist");
					OnError("An agent moved in the viewport ›› Agent identifier ‘" + agentData.agent_id + "’ does not exist");
					return;
				}

				// Get the agent corresponding to its identifier for the agents list
				Agent agent = agents[agentData.agent_id];

				// Update agent's data from the received message
				agent.latitude = agentData.pos[1];
				agent.longitude = agentData.pos[0];

				// Trigger the "agent just moved in the viewport" event callback
				OnAgentMoved(agent);
			}
		}
		#endregion

		#region WebSocket Public Events
		// Callback: the WebSocket connected
		public event Action OnConnected;

		// Callback: the WebSocket disconnected
		public event Action OnDisconnected;

		// Callback: an error has been encountered
		public event Action<string> OnError;

		// Callback: an agent entered the viewport
		public event Action<Agent> OnAgentEntered;

		// Callback: an agent left the viewport
		public event Action<Agent> OnAgentLeft;

		// Callback: an agent moved in the viewport
		public event Action<Agent> OnAgentMoved;
		#endregion

		#region Geeo Data
		// The currently connected agent (a.k.a. the current client using the Geeo SDK)
		public Agent connectedAgent {get; private set;}

		// The currently connected viewport (a.k.a. the view of the current client using the Geeo SDK)
		public Viewport connectedViewport {get; private set;}

		// Complete list of all agents currently present in the connected viewport (including the connected agent)
		private Dictionary<string, Agent> agents = new Dictionary<string, Agent>();
		public List<Agent> Agents
		{
			get
			{
				List<Agent> agentsList = new List<Agent>();

				foreach (KeyValuePair<string, Agent> agent in agents)
					agentsList.Add(agent.Value);

				return agentsList;
			}
		}
		#endregion

		#region Requests Handling
		// The Json format for a "move agent" request: { "agentPosition": [longitude, latitude] }
		private const string moveAgent_jsonFormat = "{{\"agentPosition\":[{1},{0}]}}";

		// The Json format for a "move viewport" request: { "viewPosition": [longitude1, latitude1, longitude2, latitude2] }
		private const string moveViewport_jsonFormat = "{{\"viewPosition\":[{2},{0},{3},{1}]}}";

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
			webSocket.Send(string.Format(moveAgent_jsonFormat, newLatitude, newLongitude));
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
			// Update the local currently connected agent viewport
			connectedViewport.latitude1 = newLatitude1;
			connectedViewport.latitude2 = newLatitude2;
			connectedViewport.longitude1 = newLongitude1;
			connectedViewport.longitude2 = newLongitude2;

			// Send a WebSocket message to the Geeo server to update the remote currently connected viewport location
			webSocket.Send(string.Format(moveViewport_jsonFormat, newLatitude1, newLatitude2, newLongitude1, newLongitude2));
		}
		#endregion
	}
}
