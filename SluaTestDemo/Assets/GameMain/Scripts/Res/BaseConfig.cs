using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路径选择
/// </summary>
public enum BaseOrUpdate
{
	/// <summary>
	/// 只读路径
	/// </summary>
	Base,
	/// <summary>
	/// 可读可写路径
	/// </summary>
	Update
}

/// <summary>
/// 全局配置
/// </summary>
public static class GlobalConfig
{
	/// <summary>
	/// 是否开启热更
	/// </summary>
	public static bool HotUpdate;

	/// <summary>
	/// 是否采用bundle方式加载
	/// </summary>
	public static bool BundleMode;

	/// <summary>
	/// 全局配置的构造函数
	/// </summary>
	static GlobalConfig()
	{
		HotUpdate = false;

		BundleMode = false;
	}
}

/// <summary>
/// 单个模块的配置对象
/// </summary>
public class ModuleConfig
{
	public string DownLoadURL
	{
		get
		{
			return moduleUrl + "/" + moduleName + "/" + moduleVersion;
		}
	}
	
	/// <summary>
	/// 模块的名字
	/// </summary>
	public string moduleName;

	/// <summary>
	/// 模块的版本号
	/// </summary>
	public string moduleVersion;

	/// <summary>
	/// 模块的热更服务器的地址
	/// </summary>
	public string moduleUrl;
}