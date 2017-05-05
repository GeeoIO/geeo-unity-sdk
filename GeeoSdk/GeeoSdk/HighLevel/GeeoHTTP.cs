using System;
using System.Collections;
using UnityEngine.Networking;

namespace GeeoSdk
{
	/// <summary>
	/// The SDK's HTTP module to connect to the Geeo HTTP RESTful API.
	/// </summary>
	public sealed class GeeoHTTP
	{
		#region Constructor
		// The HTTP endpoint URL
		private string httpUrl;

		/// <summary>
		/// HTTP module's constructor.
		/// </summary>
		/// <param name="_httpUrl">The HTTP endpoint URL.</param>
		public GeeoHTTP(string _httpUrl)
		{
			httpUrl = _httpUrl;
		}
		#endregion

		#region Requests Handling
		// The complete URL format for a "get guest token" request
		private const string getGuestToken_requestUrlFormat = "{0}/api/dev/token?agId={1}&viewId={2}";

		/// <summary>
		/// Get a guest token from server. Only possible with development routes allowed.
		/// </summary>
		/// <param name="agentId">The ID to use for the agent.</param>
		/// <param name="viewportId">The ID to use for the viewport.</param>
		/// <param name="OnSuccess">The callback in case of request success.</param>
		/// <param name="OnError">The callback in case of request error.</param>
		public void GetGuestToken(string agentId, string viewportId, Action<string> OnSuccess, Action<string> OnError)
		{
			Geeo.Instance.StartCoroutine(GetGuestToken_Coroutine(agentId, viewportId, OnSuccess, OnError));
		}

		/// <summary>
		/// GetGuestToken coroutine. (do not block the current thread while waiting for the request response)
		/// </summary>
		/// <param name="agentId">The ID to use for the agent.</param>
		/// <param name="viewportId">The ID to use for the viewport.</param>
		/// <param name="OnSuccess">The callback in case of request success.</param>
		/// <param name="OnError">The callback in case of request error.</param>
		private IEnumerator GetGuestToken_Coroutine(string agentId, string viewportId, Action<string> OnSuccess, Action<string> OnError)
		{
			// Build the "get guest token" request URL
			string requestUrl = string.Format(getGuestToken_requestUrlFormat, httpUrl, agentId, viewportId);
			DebugLogs.LogVerbose("[GeeoHTTP:GetGuestToken] Request URL (GET): " + requestUrl);

			// Send the request with the GET method
			using (UnityWebRequest unityWebRequest = UnityWebRequest.Get(requestUrl))
			{
				yield return unityWebRequest.Send();

				// If the request failed, call the error callback
				if (unityWebRequest.isError)
					GetGuestToken_OnError(unityWebRequest.error, OnError);
				else
				{
					// If the request failed, call the error callback
					if (unityWebRequest.responseCode != 200L)
						GetGuestToken_OnError(unityWebRequest.downloadHandler.text, OnError);
					// If the request succeeded, call the success callback
					else
					{
						DebugLogs.LogVerbose("[GeeoHTTP:GetGuestToken] Request Success: " + unityWebRequest.downloadHandler.text);
						OnSuccess(unityWebRequest.downloadHandler.text);
					}
				}
			}
		}

		/// <summary>
		/// Error callback for the GetGuestToken request.
		/// </summary>
		/// <param name="error">Error description.</param>
		/// <param name="OnError">The callback in case of request error.</param>
		private void GetGuestToken_OnError(string error, Action<string> OnError)
		{
			DebugLogs.LogError("[GeeoHTTP:GetGuestToken] Request Error: " + error);
			OnError(error);
		}
		#endregion
	}
}
