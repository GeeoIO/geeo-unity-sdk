namespace GeeoSdk
{
	/// <summary>
	/// Represents a viewport (a.k.a. a volatile squared view zone location).
	/// </summary>
	public sealed class Viewport
	{
		// Viewport's coordinates
		public double latitude1;
		public double latitude2;
		public double longitude1;
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
