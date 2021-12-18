using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SLua;

/// <summary>
/// Lua模块
/// </summary>
public class LuaManager : Singleton<LuaManager> {

	LuaSvr svr;

	public void Init(){
		svr = new LuaSvr();
		InitCustomLoaders();
	}

	/// <summary>
	/// 启动Lua脚本, 并返回table
	/// </summary>
	/// <param name="path"></param>
	public LuaTable StartLua(string path)
	{
		LuaTable self = null;
		svr.init(null, () =>
		{
			self = (LuaTable)svr.start(path);
		});
		return self;
	}

	/// <summary>
	/// 初始化自定义Loader
	/// </summary>
	private void InitCustomLoaders()
	{
		LuaState.main.loaderDelegate += LuaLoader;
	}
	
	[DoNotToLua]
	// 自定义LuaLoader
	private byte[] LuaLoader(string fn,ref string absoluteFn)
	{
		string path = "Assets/GameAssets/" + fn;
		string moduleName = fn.Split('/')[0];

		// 加载Ab资源
		TextAsset asset = AssetLoader.Instance.CreateAsset<TextAsset>(moduleName, path, MainStart.Instance.gameObject);
		if (asset != null)
		{
			//加载的lua源代码内容
			byte[] data = System.Text.Encoding.UTF8.GetBytes(asset.text);
			return data;
		}

		return null;
	}

}
