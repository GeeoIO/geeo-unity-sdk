namespace GeeoSdk
{
	/// <summary>
	/// Represents an agent (a.k.a. a volatile user point location).
	/// </summary>
	public sealed class Agent
	{
		// Agent's coordinates
		public double latitude;
		public double longitude;

		/// <summary>
		/// Converts the value of this instance to its equivalent string representation.
		/// {latitude, longitude}
		/// </summary>
		public override string ToString()
		{
			return string.Format ("{{ La: {0}, Lo: {1} }}", latitude, longitude);
		}
	}
}
