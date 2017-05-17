using LitJson;

namespace GeeoSdk
{
	/// <summary>
	/// Represents a Json "agent" type WebSocket message.
	/// </summary>
	internal sealed class AgentJson
	{
		#pragma warning disable 0649
		// Agent's identifier
		public string agent_id;

		// Agent's position: [longitude, latitude]
		public double[] pos;

		// If the agent just entered the viewport
		public bool entered;

		// If the agent just left the viewport
		public bool left;

		// Additional agent's public data
		public JsonData publicData;
		#pragma warning restore 0649

		/// <summary>
		/// Converts the value of this instance to its equivalent string representation.
		/// {agent_id, pos[1], pos[0], entered, left, publicData}
		/// </summary>
		public override string ToString()
		{
			return string.Format("{{ Id: {0}, La: {1}, Lo: {2}, Entered: {3}, Left: {4}, Data: {5} }}", agent_id, pos[1], pos[0], entered, left, publicData == null ? "null" : publicData.ToJson());
		}
	}
}
