namespace GeeoSdk
{
	/// <summary>
	/// The SDK's HTTP module to connect to the Geeo HTTP RESTful API.
	/// </summary>
	public sealed class GeeoHTTP
	{
		// The HTTP endpoint URL
		private string httpUrl;

		/// <summary>
		/// HTTP module's constructor.
		/// </summary>
		/// <param name="httpUrl">HTTP endpoint URL.</param>
		public GeeoHTTP(string _httpUrl)
		{
			httpUrl = _httpUrl;
		}
	}
}
