using UnityEngine;

namespace GeeoSdk
{
	/// <summary>
	/// Root class of the SDK. It is stateless and allows to perform basic operations with the SDK.
	/// </summary>
	public sealed class Geeo : MonoSingleton<Geeo>
	{
		// Geeo SDK's messages logging level.
		public LogLevel logLevel = LogLevel.Verbose;
	}
}
