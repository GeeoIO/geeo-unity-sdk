using LitJson;

namespace GeeoSdk
{
	/// <summary>
	/// Represents a Json "agent" type WebSocket message.
	/// </summary>
	internal sealed class AgentJson
	{
		#pragma warning disable 0649
		/// <summary>
		/// Agent's identifier.
		/// </summary>
		public string agent_id;

		/// <summary>
		/// Agent's position. (0: longitude, 1: latitude)
		/// </summary>
		public double[] pos;

		/// <summary>
		/// If the agent just entered the viewport.
		/// </summary>
		public bool entered;

		/// <summary>
		/// If the agent just left the viewport.
		/// </summary>
		public bool left;

		/// <summary>
		/// Agent's additional public data.
		/// </summary>
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
