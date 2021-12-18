using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using SLua;
using LitJson;

using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

/// <summary>
/// 模块资源加载器
/// </summary>
public class AssetLoader
{
    /// <summary>
    /// 只读路径下的资源
    /// key 模块名  value 模块所有的资源(key:资源路径，value：AssetRef)
    /// </summary>
    public Dictionary<string, Hashtable> base2Assets;

    /// <summary>
    /// 可读可写路径下的资源
    /// </summary>
    public Dictionary<string, Hashtable> update2Assets;
    
    // 单例, 继承Singleton在lua侧找不到Instance
    private static AssetLoader instance = null;
    public static AssetLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new AssetLoader();
            }
            return instance;
        }
    }
    
    public AssetLoader()
    {
        base2Assets = new Dictionary<string, Hashtable>();
        update2Assets = new Dictionary<string, Hashtable>();
    }

    /// <summary>
    /// 根据模块的json配置文件，创建内存中的容器资源
    /// </summary>
    /// <param name="moduleAbConfig"></param>
    /// <returns></returns>
    public Hashtable ConfigAssembly(ModuleABConfig moduleAbConfig)
    {
        Dictionary<string, BundleRef> name2BundleRef = new Dictionary<string, BundleRef>();

        foreach (KeyValuePair<string, BundleInfo> keyValue in moduleAbConfig.BundleArray)
        {
            string bundleName = keyValue.Key;
            BundleInfo bundleInfo = keyValue.Value;
            name2BundleRef[bundleName] = new BundleRef(bundleInfo);
        }
        
        Hashtable path2AssetRef = new Hashtable();
        for (int i = 0; i < moduleAbConfig.AssetArray.Length; i++)
        {
            AssetInfo assetInfo = moduleAbConfig.AssetArray[i];
            
            // 装配一个AssetRef对象
            AssetRef assetRef = new AssetRef(assetInfo);
            assetRef.bundleRef = name2BundleRef[assetInfo.bundle_name];
            int count = assetInfo.dependencies.Count;
            assetRef.dependencies = new BundleRef[count];

            for (int index = 0; index < count; index++)
            {
                string bundleName = assetInfo.dependencies[index];
                assetRef.dependencies[index] = name2BundleRef[bundleName];
            }
            
            // 装配好后放入path2AssetRef中
            path2AssetRef.Add(assetInfo.asset_path, assetRef);
        }

        return path2AssetRef;
    }

    /// <summary>
    /// 克隆一个GameObject对象
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public GameObject Clone(string moduleName, string path)
    {
        Debug.Log("克隆一个GameObject对象:" + path);
        
        AssetRef assetRef = LoadAssetRef<GameObject>(moduleName, path);
        if (assetRef == null || assetRef.asset == null)
        {
            return null;
        }
        
        GameObject gameObject = UnityEngine.Object.Instantiate(assetRef.asset) as  GameObject;
        if (assetRef.children == null)
        {
            assetRef.children = new List<GameObject>();
        }
        
        assetRef.children.Add(gameObject);
        return gameObject;
    }
    
    
    /// <summary>
    /// 加载 AssetRef 对象
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="assetPath"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private AssetRef LoadAssetRef<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if (GlobalConfig.BundleMode == false)
        {
            return LoadAssetRef_Editor<T>(moduleName, assetPath);
        }
        else
        {
            return LoadAssetRef_Runtime<T>(moduleName, assetPath);
        }
#else
        return LoadAssetRef_Runtime<T>(moduleName, assetPath);
#endif
    }
    
    /// <summary>
    /// 根据type加载 AssetRef 对象
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="assetPath"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private AssetRef LoadAssetRef(string moduleName, string assetPath, Type type)
    {
#if UNITY_EDITOR
        if (GlobalConfig.BundleMode == false)
        {
            return LoadAssetRef_Editor(moduleName, assetPath, type);
        }
        else
        {
            return LoadAssetRef_Runtime(moduleName, assetPath, type);
        }
#else
        return LoadAssetRef_Runtime(moduleName, assetPath, type);
#endif
    }

    /// <summary>
    /// 编辑器下加载 AssetRef 对象
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="assetPath"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Editor<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }
        AssetRef assetRef = new AssetRef(null);
        assetRef.asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
        return assetRef;
#else
        return null;
#endif
    } 

    /// <summary>
    /// 编辑器下加载 AssetRef 对象, 根据type
    /// </summary>
    private AssetRef LoadAssetRef_Editor(string moduleName, string assetPath, Type type)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }
        AssetRef assetRef = new AssetRef(null);
        assetRef.asset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, type);
        return assetRef;
#else
        return null;
#endif
    } 
    
    /// <summary>
    /// AB包加载 AssetRef 对象
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="assetPath"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Runtime<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        // 先查找update路径下的容器，再查找base路径下的容器
        BaseOrUpdate witch = BaseOrUpdate.Update;
        
        // 获取对应模块的所有AssetRef信息
        Hashtable module2AssetRef;
        if (update2Assets.TryGetValue(moduleName, out module2AssetRef) == false)
        {
            witch = BaseOrUpdate.Base;
            if (base2Assets.TryGetValue(moduleName, out module2AssetRef) == false)
            {
                Debug.LogError("未找到资源对应的模块：moduleName" + moduleName);
                return null;
            }
        }
        
        AssetRef assetRef = (AssetRef) module2AssetRef[assetPath];
        if (assetRef == null)
        {
            Debug.LogError("未找到资源：moduleName" + moduleName + " path:" + assetPath);
            return null;
        }

        if (assetRef.asset != null)
        {
            return assetRef;
        }
        
        // 1.处理assetRef依赖的BundleRef列表
        foreach (BundleRef oneBundleRef in assetRef.dependencies)
        {
            if (oneBundleRef.bundle == null)
            {
                string bundlePath = BundlePath(witch,moduleName, oneBundleRef.bundleInfo.bundle_name);
                oneBundleRef.bundle = AssetBundle.LoadFromFile(bundlePath);
            }

            if (oneBundleRef.children == null)
            {
                oneBundleRef.children = new List<AssetRef>();
            }
            oneBundleRef.children.Add(assetRef);
        }

        // 2.处理assetRef属于的BundleRef对象
        BundleRef bundleRef = assetRef.bundleRef;

        if (bundleRef.bundle == null)
        {
            bundleRef.bundle = AssetBundle.LoadFromFile(BundlePath(witch,moduleName, bundleRef.bundleInfo.bundle_name));
        }

        if (bundleRef.children == null)
        {
            bundleRef.children = new List<AssetRef>();
        }
        bundleRef.children.Add(assetRef);
        
        // 3. 从bundle中提取asset
        assetRef.asset = assetRef.bundleRef.bundle.LoadAsset<T>(assetRef.assetInfo.asset_path);
        if (typeof(T) == typeof(GameObject) && assetRef.assetInfo.asset_path.EndsWith(".prefab"))
        {
            assetRef.isGameObject = true;
        }
        else
        {
            assetRef.isGameObject = false;
        }
        
        return assetRef;
    }

    /// <summary>
    /// AB包加载 AssetRef 对象, 根据type
    /// </summary>
    private AssetRef LoadAssetRef_Runtime(string moduleName, string assetPath, Type type)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        // 先查找update路径下的容器，再查找base路径下的容器
        BaseOrUpdate witch = BaseOrUpdate.Update;
        
        // 获取对应模块的所有AssetRef信息
        Hashtable module2AssetRef;
        if (update2Assets.TryGetValue(moduleName, out module2AssetRef) == false)
        {
            witch = BaseOrUpdate.Base;
            if (base2Assets.TryGetValue(moduleName, out module2AssetRef) == false)
            {
                Debug.LogError("未找到资源对应的模块：moduleName" + moduleName);
                return null;
            }
        }
        
        AssetRef assetRef = (AssetRef) module2AssetRef[assetPath];
        if (assetRef == null)
        {
            Debug.LogError("未找到资源：moduleName" + moduleName + " path:" + assetPath);
            return null;
        }

        if (assetRef.asset != null)
        {
            return assetRef;
        }
        
        // 1.处理assetRef依赖的BundleRef列表
        foreach (BundleRef oneBundleRef in assetRef.dependencies)
        {
            if (oneBundleRef.bundle == null)
            {
                string bundlePath = BundlePath(witch,moduleName, oneBundleRef.bundleInfo.bundle_name);
                oneBundleRef.bundle = AssetBundle.LoadFromFile(bundlePath);
            }

            if (oneBundleRef.children == null)
            {
                oneBundleRef.children = new List<AssetRef>();
            }
            oneBundleRef.children.Add(assetRef);
        }

        // 2.处理assetRef属于的BundleRef对象
        BundleRef bundleRef = assetRef.bundleRef;

        if (bundleRef.bundle == null)
        {
            bundleRef.bundle = AssetBundle.LoadFromFile(BundlePath(witch,moduleName, bundleRef.bundleInfo.bundle_name));
        }

        if (bundleRef.children == null)
        {
            bundleRef.children = new List<AssetRef>();
        }
        bundleRef.children.Add(assetRef);
        
        // 3. 从bundle中提取asset
        assetRef.asset = assetRef.bundleRef.bundle.LoadAsset(assetRef.assetInfo.asset_path, type);
        if (type == typeof(GameObject) && assetRef.assetInfo.asset_path.EndsWith(".prefab"))
        {
            assetRef.isGameObject = true;
        }
        else
        {
            assetRef.isGameObject = false;
        }
        
        return assetRef;
    }
    
    /// <summary>
    /// 创建资源对象，并且将其赋予游戏对象gameObject
    /// </summary>
    /// <param name="moduleName">资源名</param>
    /// <param name="assetPath">资源路径</param>
    /// <param name="gameObject">资源加载后要挂载到的游戏对象</param>
    /// <typeparam name="T">资源类型</typeparam>
    /// <returns></returns>
    [DoNotToLua]
    public T CreateAsset<T>(string moduleName, string assetPath, GameObject gameObject) where T : UnityEngine.Object
    {
        if (typeof(T) == typeof(GameObject) || (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab")))
        {
            Debug.LogError("不可以加载GameObject类型，请直接使用AssetLoader.Instance.Clone接口，path：" + assetPath);
            return null;
        }

        if (gameObject == null)
        {
            Debug.LogError("CreateAsset必须要传递一个需要挂载的GameObject对象！");
            return null;
        }

        AssetRef assetRef = LoadAssetRef<T>(moduleName, assetPath);
        if (assetRef == null || assetRef.asset == null)
        {
            return null;
        }

        if (assetRef.children == null)
        {
            assetRef.children = new List<GameObject>();
        }
        assetRef.children.Add(gameObject);

        return assetRef.asset as T;
    }

    /// <summary>
    /// 创建资源对象，提供给Lua侧调用
    /// </summary>
    public Object LuaCreateAsset(string moduleName, string assetPath, GameObject gameObject, Type type)
    {
        Debug.Log("Lua调用CreateAsset");
        if (type == typeof(GameObject) || (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab")))
        {
            Debug.LogError("不可以加载GameObject类型，请直接使用AssetLoader.Instance.Clone接口，path：" + assetPath);
            return null;
        }

        if (gameObject == null)
        {
            Debug.LogError("CreateAsset必须要传递一个需要挂载的GameObject对象！");
            return null;
        }

        AssetRef assetRef = LoadAssetRef(moduleName, assetPath, type);
        if (assetRef == null || assetRef.asset == null)
        {
            return null;
        }

        if (assetRef.children == null)
        {
            assetRef.children = new List<GameObject>();
        }
        assetRef.children.Add(gameObject);

        return assetRef.asset;
    }
    
    /// <summary>
    /// 创建GameObject对象，提供给Lua侧调用
    /// </summary>
    public Object LuaClone(string moduleName, string assetPath)
    {
        Debug.Log("Lua调用LuaClone");
        return Clone(moduleName,assetPath);
    }
    
    /// <summary>
    /// 全局卸载函数
    /// </summary>
    /// <param name="module2Assets"></param>
    public void UnLoad(Dictionary<string, Hashtable> module2Assets)
    {
        foreach (string moduleName in module2Assets.Keys)
        {
            Hashtable Path2AssetRef = module2Assets[moduleName];
            if (Path2AssetRef == null)
            {
                Debug.LogError("Path2AssetRef为空，moduleName："+ moduleName);
                continue;
            }

            foreach (AssetRef assetRef in Path2AssetRef.Values)
            {
                if (assetRef.children == null || assetRef.children.Count == 0)
                {
                    continue;
                }

                for (int i = assetRef.children.Count - 1; i >= 0; i--)
                {
                    GameObject obj = assetRef.children[i];
                    if (obj == null)
                    {
                        assetRef.children.RemoveAt(i);
                    }
                }
                
                // 如果这个资源assetRef已经没有被任何GameObject所依赖了，那么此assetRef就可以卸载了
                if (assetRef.children.Count == 0)
                {
                    assetRef.asset = null;
                    Resources.UnloadUnusedAssets();
                    
                    // 对于assetRef所属的这个bundle解除关系
                    assetRef.bundleRef.children.Remove(assetRef);
                    if (assetRef.bundleRef.children.Count == 0)
                    {
                        assetRef.bundleRef.bundle.Unload(true);
                    }
                    
                    // 对于assetRef所依赖的bundle列表解除关系
                    foreach (BundleRef bundleRef in assetRef.dependencies)
                    {
                        bundleRef.children.Remove(assetRef);
                        if (bundleRef.children.Count == 0)
                        {
                            bundleRef.bundle.Unload(true);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 加载模块对应的json文件
    /// </summary>
    /// <param name="baseOrUpdate"></param>
    /// <param name="moduleName"></param>
    /// <param name="bundleConfigName"></param>
    /// <returns></returns>
    public async Task<ModuleABConfig> LoadAssetBundleConfig(BaseOrUpdate baseOrUpdate,string moduleName, string bundleConfigName)
    {
        string url = BundlePath(baseOrUpdate, moduleName, bundleConfigName);

        // 此处必须要加上"file://"
        UnityWebRequest request = UnityWebRequest.Get("file://" + url);
        await request.SendWebRequest();

        if (string.IsNullOrEmpty(request.error) == true)
        {
            return JsonMapper.ToObject<ModuleABConfig>(request.downloadHandler.text);
        }
        return null;
    }

    /// <summary>
    /// 工具函数， 获取bundle路径
    /// </summary>
    /// <param name="baseOrUpdate"></param>
    /// <param name="moduleName"></param>
    /// <param name="bundleName"></param>
    /// <returns></returns>
    private string BundlePath(BaseOrUpdate baseOrUpdate ,string moduleName, string bundleName)
    {
        if (baseOrUpdate == BaseOrUpdate.Update)
        {
            return Application.persistentDataPath + "/Bundles/" + moduleName + "/" + bundleName;
        }
        else
        {
            return Application.streamingAssetsPath + "/" + moduleName + "/" + bundleName;
        }
    }
}
