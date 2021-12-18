using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AB包
/// </summary>
public class BundleRef
{
	/// <summary>
	/// 对应的bundle配置信息
	/// </summary>
	public BundleInfo bundleInfo;
	
	/// <summary>
	/// 加载到内存的bundle对象
	/// </summary>
	public AssetBundle bundle;

	/// <summary>
	/// BundleRef对象被哪些AssetRef对象依赖
	/// </summary>
	public List<AssetRef> children;
	
	public BundleRef(BundleInfo bundleInfo)
	{
		this.bundleInfo = bundleInfo;
	}
}
