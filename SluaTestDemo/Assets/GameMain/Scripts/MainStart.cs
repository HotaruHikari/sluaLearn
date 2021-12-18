using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using UnityEngine.UI;

/// <summary>
/// 游戏入口
/// </summary>
public class MainStart : MonoBehaviour {

	/// <summary>
	/// 主Mono对象
	/// </summary>
	public static MainStart Instance;

	public bool HotUpdate;
	public bool BundleMode;
	
	public async void Awake()
	{
		InitGlobal();
		// 启动Launch模块
		ModuleConfig launchModule = new ModuleConfig()
		{
			moduleName = "Launch",
			moduleVersion = "20211212",
			moduleUrl = "http://127.0.0.1"
		};

		bool result = await ModuleManager.Instance.Load(launchModule);

		if (result)
		{
			// 资源加载测试代码
			/*AssetLoader.Instance.Clone("Launch", "Assets/GameAssets/Launch/Sphere.prefab");
			var obj = AssetLoader.Instance.Clone("Launch", "Assets/GameAssets/Launch/New Sprite.prefab");
			Sprite sp = AssetLoader.Instance.CreateAsset<Sprite>("Launch", "Assets/GameAssets/Launch/Sprite/Square.png", obj);
			obj.GetComponent<SpriteRenderer>().sprite = sp;*/
			
			Debug.Log("Lua 代码开始...");
			
			// Lua 逻辑测试代码
			LuaManager.Instance.Init();
			GameObject go = new GameObject("LuaMain");
			go.AddComponent<LuaBehaviour>().SetLuaScript("Launch/Lua/Main.txt");
			
		}
	}

	/// <summary>
	/// 初始化全局变量
	/// </summary>
	private void InitGlobal()
	{
		Instance = this;
		GlobalConfig.HotUpdate = this.HotUpdate;
		GlobalConfig.BundleMode = this.BundleMode;
		DontDestroyOnLoad(gameObject);
	}

	private void Update()
	{
		// 卸载测试
		//AssetLoader.Instance.UnLoad(AssetLoader.Instance.base2Assets);
		
	}
}
