using LitJson;

namespace GeeoSdk
{
	/// <summary>
	/// Represents a Json "point of interest update" type WebSocket message.
	/// </summary>
	internal sealed class PointOfInterestJson
	{
		#pragma warning disable 0649
		/// <summary>
		/// Point of interest's identifier.
		/// </summary>
		public string poi_id;

		/// <summary>
		/// Point of interest's position. (0: longitude, 1: latitude)
		/// </summary>
		public double[] pos;

		/// <summary>
		/// If the point of interest just entered the viewport.
		/// </summary>
		public bool entered;

		/// <summary>
		/// If the point of interest just left the viewport.
		/// </summary>
		public bool left;

		/// <summary>
		/// Point of interest's additional public data.
		/// </summary>
		public JsonData publicData;

		/// <summary>
		/// Point of interest's creator identifier.
		/// </summary>
		public string creator;
		#pragma warning restore 0649

		/// <summary>
		/// Converts the value of this instance to its equivalent string representation.
		/// {poi_id, pos[1], pos[0], entered, left, publicData, creator}
		/// </summary>
		public override string ToString()
		{
			return string.Format("{{ Id: {0}, La: {1}, Lo: {2}, Entered: {3}, Left: {4}, Data: {5}, Creator: {6} }}", poi_id, pos[1], pos[0], entered, left, publicData == null ? "null" : publicData.ToJson(), creator);
		}
	}
}
