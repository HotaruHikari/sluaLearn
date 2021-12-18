using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 模块管理 工具类
/// </summary>
public class ModuleManager : Singleton<ModuleManager> {

	/// <summary>
	/// 加载一个模块
	/// </summary>
	/// <param name="moduleConfig"></param>
	/// <returns></returns>
	public async Task<bool> Load(ModuleConfig moduleConfig)
	{
		if (GlobalConfig.HotUpdate == false)
		{
			if (GlobalConfig.BundleMode == false)
			{
				return true;
			}
			else
			{
				return await LoadBase(moduleConfig.moduleName);
			}
		}
		else
		{
			if (await Downloader.Instance.Download(moduleConfig) == false)
			{
				return false;
			}

			bool baseOk = await LoadBase(moduleConfig.moduleName);
			bool updateOk = await LoadUpdate(moduleConfig.moduleName);

			if (baseOk == false && updateOk == false)
			{
				return false;
			}

			return true;
		}
	}

	private async Task<bool> LoadBase(string moduleName)
	{
		ModuleABConfig moduleAbConfig =
			await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Base, moduleName, moduleName.ToLower() + ".json");
		if (moduleAbConfig == null)
		{
			return false;
		}
		
		Debug.Log($"模块{moduleName}的只读路径包含的AB包总数量:{moduleAbConfig.BundleArray.Count}");
		Hashtable Path2AssetRef = AssetLoader.Instance.ConfigAssembly(moduleAbConfig);
		AssetLoader.Instance.base2Assets.Add(moduleName, Path2AssetRef);
		return true;
	}

	private async Task<bool> LoadUpdate(string moduleName)
	{
		ModuleABConfig moduleAbConfig =
			await AssetLoader.Instance.LoadAssetBundleConfig(BaseOrUpdate.Update, moduleName, moduleName.ToLower() + ".json");
		if (moduleAbConfig == null)
		{
			return false;
		}
		
		Debug.Log($"模块{moduleName}的可读可写路径包含的AB包总数量:{moduleAbConfig.BundleArray.Count}");
		Hashtable Path2AssetRef = AssetLoader.Instance.ConfigAssembly(moduleAbConfig);
		AssetLoader.Instance.update2Assets.Add(moduleName, Path2AssetRef);
		return true;
	}
}
