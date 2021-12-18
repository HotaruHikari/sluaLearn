using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{

	private static T instance;

	public static T Instance
	{
		get
		{
			if (instance == null)
			{
				instance = GameObject.FindObjectOfType<T>();
				if (instance == null)
				{
					GameObject obj = new GameObject(typeof(T).FullName);
					instance = obj.AddComponent<T>();
					GameObject.DontDestroyOnLoad(obj);
				}
			}
			return instance;
		}
	}
}
