using UnityEngine;

using GeeoSdk;

public class DemoScript : MonoBehaviour
{
	private void Start()
	{
		if (Geeo.HasInstance == false)
		{
			Debug.LogError("[DemoScript:Start] No Geeo instance found ›› Please attach a ‘Geeo’ component on an active object of your scene!");
			return;
		}
	}
}
