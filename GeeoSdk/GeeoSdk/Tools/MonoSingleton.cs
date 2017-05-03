using UnityEngine;

namespace Geeo
{
	/// <summary>
	/// A class derivated from MonoBehaviour and following the singleton design pattern.
	/// </summary>
	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		#region Instance Handling
		// The singleton instance
		private static MonoSingleton<T> instance = null;

		/// <summary>
		/// Register the singleton instance at Awake.
		/// </summary>
		protected virtual void Awake()
		{
			if (instance == null)
				instance = this;
			else
			{
				DebugLogs.LogError("[MonoSingleton:Awake] Found more than one instance of " + this.GetType().Name + " ›› Destroying the last one");
				Destroy(this);
			}
		}

		/// <summary>
		/// Get the singleton instance.
		/// </summary>
		public static T Instance
		{
			get
			{
				if (instance != null)
					return instance as T;
				else
				{
					DebugLogs.LogError("[MonoSingleton:InstanceGet] Not found any instance of " + typeof(T).Name + " ›› Returning null");
					return null;
				}
			}
		}

		/// <summary>
		/// Check if a singleton instance is registered.
		/// </summary>
		public static bool HasInstance
		{
			get { return instance != null; }
		}
		#endregion
	}
}
