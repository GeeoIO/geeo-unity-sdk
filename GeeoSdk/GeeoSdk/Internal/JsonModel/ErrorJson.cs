namespace GeeoSdk
{
	/// <summary>
	/// Represents a Json "error report" type WebSocket message.
	/// </summary>
	internal sealed class ErrorJson
	{
		#pragma warning disable 0649
		/// <summary>
		/// Error's type.
		/// </summary>
		public string error;

		/// <summary>
		/// Error's optional message.
		/// </summary>
		public string message;
		#pragma warning restore 0649

		/// <summary>
		/// Converts the value of this instance to its equivalent string representation.
		/// {error, message}
		/// </summary>
		public override string ToString()
		{
			return string.Format("{{ Error: {0}, Message: {1} }}", error, message == null ? "null" : message);
		}
	}
}
