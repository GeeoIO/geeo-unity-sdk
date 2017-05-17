namespace GeeoSdk
{
	/// <summary>
	/// Represents a viewport (a.k.a. a volatile squared view zone location).
	/// </summary>
	public sealed class Viewport
	{
		/// <summary>
		/// Viewport's first latitude.
		/// </summary>
		public double latitude1;

		/// <summary>
		/// Viewport's second latitude.
		/// </summary>
		public double latitude2;

		/// <summary>
		/// Viewport's first longitude.
		/// </summary>
		public double longitude1;

		/// <summary>
		/// Viewport's second longitude.
		/// </summary>
		public double longitude2;

		/// <summary>
		/// Converts the value of this instance to its equivalent string representation.
		/// {latitude1, latitude2, longitude1, longitude2}
		/// </summary>
		public override string ToString()
		{
			return string.Format("{{ La1: {0}, La2: {1}, Lo1: {2}, Lo2: {3} }}", latitude1, latitude2, longitude1, longitude2);
		}
	}
}
