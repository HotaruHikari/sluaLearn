using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Res : MonoBehaviour {

	// Use this for initialization
	async void Start () {
		UnityWebRequest request = UnityWebRequest.Get("file://C:/Users/dell/AppData/LocalLow/DefaultCompany/SluaTestDemo/Bundles/Launch/launch_temp.json");
		await request.SendWebRequest();
		var t = request.error;
		Debug.LogWarning(t);
		
		UnityWebRequest request2 = UnityWebRequest.Get("E:/123.txt");
		await request2.SendWebRequest();
		var t2 = request2.error;
		Debug.LogWarning(t2 + request2.downloadHandler.text);
		
		UnityWebRequest request3 = UnityWebRequest.Get(Application.streamingAssetsPath +"/Launch/launch.json");
		await request3.SendWebRequest();
		var t3 = request3.error;
		Debug.LogWarning(t3);

		var temp = File.ReadAllBytes("C:/Users/dell/AppData/LocalLow/DefaultCompany/SluaTestDemo/Bundles/Launch/launch_temp.json");
		Debug.Log(temp.Length);
		
		Debug.Log(Application.streamingAssetsPath);
		Debug.Log(Application.persistentDataPath);

		temp = File.ReadAllBytes(Application.streamingAssetsPath +"/Launch/launch.json");
		Debug.Log(temp.Length);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
