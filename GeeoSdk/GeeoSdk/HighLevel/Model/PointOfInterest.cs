using LitJson;

namespace GeeoSdk
{
	/// <summary>
	/// Represents a point of interest (a.k.a. a persistent interest point location).
	/// </summary>
	public sealed class PointOfInterest
	{
		/// <summary>
		/// Point of interest's identifier.
		/// </summary>
		public string id;

		/// <summary>
		/// Point of interest's latitude.
		/// </summary>
		public double latitude;

		/// <summary>
		/// Point of interest's longitude.
		/// </summary>
		public double longitude;

		/// <summary>
		/// Point of interest's public data.
		/// </summary>
		public JsonData publicData;

		/// <summary>
		/// Point of interest's creator identifier.
		/// </summary>
		public string creatorId;

		/// <summary>
		/// Converts the value of this instance to its equivalent string representation.
		/// {id, latitude, longitude, publicData, creatorId}
		/// </summary>
		public override string ToString()
		{
			return string.Format("{{ Id: {0}, La: {1}, Lo: {2}, Data: {3}, Creator: {4} }}", id, latitude, longitude, publicData == null ? "null" : publicData.ToJson(), creatorId);
		}
	}
}
