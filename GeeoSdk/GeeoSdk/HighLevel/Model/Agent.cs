using LitJson;

namespace GeeoSdk
{
	/// <summary>
	/// Represents an agent (a.k.a. a volatile user point location).
	/// </summary>
	public sealed class Agent
	{
		// Agent's identifier
		public string id;

		// Agent's coordinates
		public double latitude;
		public double longitude;

		// Agent's public data
		public JsonData publicData;

		/// <summary>
		/// Converts the value of this instance to its equivalent string representation.
		/// {id, latitude, longitude, publicData}
		/// </summary>
		public override string ToString()
		{
			return string.Format("{{ Id: {0}, La: {1}, Lo: {2}, Data: {3} }}", id, latitude, longitude, publicData == null ? "null" : publicData.ToJson());
		}
	}
}
