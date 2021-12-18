using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内存中的单个资源对象
/// </summary>
public class AssetRef
{
	/// <summary>
	/// 资源的配置信息
	/// </summary>
	public AssetInfo assetInfo;

	/// <summary>
	/// 资源所属的BundleRef对象
	/// </summary>
	public BundleRef bundleRef;

	/// <summary>
	/// 资源所依赖的BundleRef对象列表
	/// </summary>
	public BundleRef[] dependencies;

	/// <summary>
	/// 从bundle文件中提取出的资源对象
	/// </summary>
	public Object asset;

	/// <summary>
	/// 资源是否是prefab
	/// </summary>
	public bool isGameObject;

	/// <summary>
	/// AssetRef对象被哪些实例化的GameObject依赖
	/// </summary>
	public List<GameObject> children;

	public AssetRef(AssetInfo assetInfo)
	{
		this.assetInfo = assetInfo;
	}
}
